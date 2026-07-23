using GuardLan.Application.Models;
using GuardLan.Application.Suricata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json;

namespace GuardLan.Infrastructure.Suricata;

public sealed class SuricataEveJsonSource : ISuricataAlertSource
{
    private static readonly JsonSerializerOptions CheckpointJsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly SuricataEveOptions options;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<SuricataEveJsonSource> logger;

    public SuricataEveJsonSource(
        IConfiguration configuration,
        TimeProvider timeProvider,
        ILogger<SuricataEveJsonSource> logger)
    {
        options = SuricataEveOptions.FromConfiguration(configuration);
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public bool IsEnabled => options.Enabled;

    public string SourceName => "Suricata eve.json";

    public async Task<SuricataAlertReadResult> ReadNewAlertsAsync(
        CancellationToken cancellationToken = default)
    {
        var sourcePath = NormalizeConfiguredPath(options.Path);
        if (sourcePath.Length == 0)
        {
            return Unavailable("Suricata eve.json path is not configured.");
        }

        if (!File.Exists(sourcePath))
        {
            return Unavailable($"Suricata eve.json was not found at {sourcePath}.");
        }

        var checkpoint = await LoadCheckpointAsync(sourcePath, cancellationToken);
        if (!options.ReadFromBeginning && checkpoint is null)
        {
            var lineCount = await CountLinesAsync(sourcePath, cancellationToken);

            return Available(
                LinesRead: 0,
                RecordsParsed: 0,
                SkippedInvalid: 0,
                Records: [],
                Checkpoint: CreateCheckpoint(sourcePath, lineCount),
                $"Checkpointed existing Suricata eve.json content at line {lineCount}.");
        }

        var startLine = checkpoint?.LineNumber ?? 0;
        var readAttempt = await ReadFromLineAsync(sourcePath, startLine, cancellationToken);

        if (readAttempt.TotalLines < startLine)
        {
            readAttempt = await ReadFromLineAsync(sourcePath, startLine: 0, cancellationToken);
        }

        var nextLine = readAttempt.LastProcessedLine > 0
            ? readAttempt.LastProcessedLine
            : readAttempt.TotalLines;
        var message = readAttempt.LinesRead == 0
            ? "No new Suricata alert rows were found."
            : $"Read {readAttempt.LinesRead} Suricata rows and parsed {readAttempt.RecordsParsed} alerts.";

        return Available(
            readAttempt.LinesRead,
            readAttempt.RecordsParsed,
            readAttempt.SkippedInvalid,
            readAttempt.Records,
            CreateCheckpoint(sourcePath, nextLine),
            message);
    }

    public async Task SaveCheckpointAsync(
        SuricataLogCheckpoint checkpoint,
        CancellationToken cancellationToken = default)
    {
        var checkpointPath = ResolveCheckpointPath(checkpoint.SourcePath);
        var directory = Path.GetDirectoryName(checkpointPath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = new FileStream(
            checkpointPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.Read,
            bufferSize: 4096,
            useAsync: true);

        await JsonSerializer.SerializeAsync(
            stream,
            checkpoint,
            CheckpointJsonOptions,
            cancellationToken);
    }

    private async Task<ReadAttempt> ReadFromLineAsync(
        string sourcePath,
        int startLine,
        CancellationToken cancellationToken)
    {
        var currentLine = 0;
        var lastProcessedLine = 0;
        var linesRead = 0;
        var skippedInvalid = 0;
        var records = new List<IdsAlertIngestionRecordDto>();

        await using var stream = new FileStream(
            sourcePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            bufferSize: 4096,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var reader = new StreamReader(stream);

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            currentLine++;

            if (currentLine <= startLine || string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (linesRead >= options.MaxRecords)
            {
                break;
            }

            linesRead++;
            lastProcessedLine = currentLine;

            if (TryParseAlert(line, out var record))
            {
                records.Add(record);
            }
            else
            {
                skippedInvalid++;
            }
        }

        return new ReadAttempt(
            currentLine,
            lastProcessedLine,
            linesRead,
            records.Count,
            skippedInvalid,
            records);
    }

    private static bool TryParseAlert(string line, out IdsAlertIngestionRecordDto record)
    {
        try
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;

            if (!TextEquals(GetString(root, "event_type"), "alert") ||
                !TryGetObject(root, out var alert, "alert") ||
                !TryParseTimestamp(GetString(root, "timestamp"), out var timestampUtc))
            {
                return Fail(out record);
            }

            var signature = GetString(alert, "signature");
            if (string.IsNullOrWhiteSpace(signature))
            {
                return Fail(out record);
            }

            var sourceIp = GetString(root, "src_ip");
            var destinationIp = GetString(root, "dest_ip");
            var sourcePort = GetNullableInt(root, "src_port");
            var destinationPort = GetNullableInt(root, "dest_port");
            var protocol = GetString(root, "proto");
            var appProtocol = GetString(root, "app_proto");
            var flowId = GetString(root, "flow_id");
            var signatureId = GetString(alert, "signature_id");
            var category = GetString(alert, "category");
            var action = GetString(alert, "action");
            var severity = GetNullableInt(alert, "severity");

            record = new IdsAlertIngestionRecordDto
            {
                SourceRecordId = BuildSourceRecordId(flowId, signatureId, timestampUtc),
                SourceIp = sourceIp,
                DestinationIp = destinationIp,
                SourcePort = sourcePort,
                DestinationPort = destinationPort,
                Protocol = protocol,
                Signature = signature,
                Category = category,
                Severity = severity,
                Action = action,
                EvidenceSummary = BuildEvidenceSummary(
                    signatureId,
                    category,
                    action,
                    sourceIp,
                    sourcePort,
                    destinationIp,
                    destinationPort,
                    protocol,
                    appProtocol),
                TimestampUtc = timestampUtc
            };

            return true;
        }
        catch (JsonException)
        {
            return Fail(out record);
        }
    }

    private async Task<int> CountLinesAsync(string sourcePath, CancellationToken cancellationToken)
    {
        var lineCount = 0;

        await using var stream = new FileStream(
            sourcePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            bufferSize: 4096,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var reader = new StreamReader(stream);

        while (await reader.ReadLineAsync(cancellationToken) is not null)
        {
            lineCount++;
        }

        return lineCount;
    }

    private async Task<SuricataLogCheckpoint?> LoadCheckpointAsync(
        string sourcePath,
        CancellationToken cancellationToken)
    {
        var checkpointPath = ResolveCheckpointPath(sourcePath);
        if (!File.Exists(checkpointPath))
        {
            return null;
        }

        try
        {
            await using var stream = new FileStream(
                checkpointPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                bufferSize: 4096,
                useAsync: true);
            var checkpoint = await JsonSerializer.DeserializeAsync<SuricataLogCheckpoint>(
                stream,
                cancellationToken: cancellationToken);

            if (checkpoint is null ||
                !string.Equals(checkpoint.SourcePath, sourcePath, StringComparison.OrdinalIgnoreCase) ||
                checkpoint.LineNumber < 0)
            {
                return null;
            }

            return checkpoint;
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Could not read Suricata checkpoint {CheckpointPath}.", checkpointPath);
            return null;
        }
        catch (IOException exception)
        {
            logger.LogWarning(exception, "Could not read Suricata checkpoint {CheckpointPath}.", checkpointPath);
            return null;
        }
    }

    private SuricataLogCheckpoint CreateCheckpoint(string sourcePath, int lineNumber)
    {
        return new SuricataLogCheckpoint(
            sourcePath,
            Math.Max(0, lineNumber),
            timeProvider.GetUtcNow().UtcDateTime);
    }

    private string ResolveCheckpointPath(string sourcePath)
    {
        var configuredPath = NormalizeConfiguredPath(options.CheckpointPath);

        return configuredPath.Length == 0
            ? $"{sourcePath}.guardlan-checkpoint.json"
            : configuredPath;
    }

    private static string? BuildSourceRecordId(string? flowId, string? signatureId, DateTime timestampUtc)
    {
        if (string.IsNullOrWhiteSpace(flowId) && string.IsNullOrWhiteSpace(signatureId))
        {
            return null;
        }

        return string.Join(
            ":",
            "flow",
            flowId ?? "unknown",
            "signature",
            signatureId ?? "unknown",
            timestampUtc.Ticks);
    }

    private static string BuildEvidenceSummary(
        string? signatureId,
        string? category,
        string? action,
        string? sourceIp,
        int? sourcePort,
        string? destinationIp,
        int? destinationPort,
        string? protocol,
        string? appProtocol)
    {
        var endpoint = $"{sourceIp ?? "unknown"}:{sourcePort?.ToString() ?? "-"} -> {destinationIp ?? "unknown"}:{destinationPort?.ToString() ?? "-"}";
        var metadata = new[]
            {
                string.IsNullOrWhiteSpace(signatureId) ? null : $"signature_id={signatureId}",
                string.IsNullOrWhiteSpace(category) ? null : $"category={category}",
                string.IsNullOrWhiteSpace(action) ? null : $"action={action}",
                string.IsNullOrWhiteSpace(protocol) ? null : $"proto={protocol}",
                string.IsNullOrWhiteSpace(appProtocol) ? null : $"app_proto={appProtocol}"
            }
            .Where(value => value is not null);

        return $"{endpoint}; {string.Join("; ", metadata)}";
    }

    private static bool TryParseTimestamp(string? value, out DateTime timestampUtc)
    {
        if (DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
        {
            timestampUtc = parsed.UtcDateTime;
            return true;
        }

        timestampUtc = default;
        return false;
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!TryGetProperty(element, out var property, propertyName))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }

    private static int? GetNullableInt(JsonElement element, string propertyName)
    {
        if (!TryGetProperty(element, out var property, propertyName))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number &&
            property.TryGetInt32(out var number))
        {
            return number;
        }

        return property.ValueKind == JsonValueKind.String &&
               int.TryParse(property.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static bool TryGetObject(JsonElement element, out JsonElement value, string propertyName)
    {
        return TryGetProperty(element, out value, propertyName) &&
               value.ValueKind == JsonValueKind.Object;
    }

    private static bool TryGetProperty(JsonElement element, out JsonElement value, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            value = default;
            return false;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static bool TextEquals(string? left, string right)
    {
        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    private static bool Fail(out IdsAlertIngestionRecordDto record)
    {
        record = new IdsAlertIngestionRecordDto();
        return false;
    }

    private static SuricataAlertReadResult Unavailable(string message)
    {
        return new SuricataAlertReadResult(
            SourceAvailable: false,
            LinesRead: 0,
            RecordsParsed: 0,
            SkippedInvalid: 0,
            Records: [],
            Checkpoint: null,
            message);
    }

    private static SuricataAlertReadResult Available(
        int LinesRead,
        int RecordsParsed,
        int SkippedInvalid,
        IReadOnlyList<IdsAlertIngestionRecordDto> Records,
        SuricataLogCheckpoint? Checkpoint,
        string Message)
    {
        return new SuricataAlertReadResult(
            SourceAvailable: true,
            LinesRead,
            RecordsParsed,
            SkippedInvalid,
            Records,
            Checkpoint,
            Message);
    }

    private static string NormalizeConfiguredPath(string? configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return string.Empty;
        }

        return Path.GetFullPath(configuredPath.Trim());
    }

    private sealed record ReadAttempt(
        int TotalLines,
        int LastProcessedLine,
        int LinesRead,
        int RecordsParsed,
        int SkippedInvalid,
        IReadOnlyList<IdsAlertIngestionRecordDto> Records);

    private sealed class SuricataEveOptions
    {
        public bool Enabled { get; init; }

        public string Path { get; init; } = string.Empty;

        public string CheckpointPath { get; init; } = string.Empty;

        public int MaxRecords { get; init; } = 5000;

        public bool ReadFromBeginning { get; init; } = true;

        public static SuricataEveOptions FromConfiguration(IConfiguration configuration)
        {
            var section = configuration.GetSection("Suricata:EveLog");

            return new SuricataEveOptions
            {
                Enabled = ReadBoolean(section, "Enabled", defaultValue: false),
                Path = ReadString(section, "Path", string.Empty),
                CheckpointPath = ReadString(section, "CheckpointPath", string.Empty),
                MaxRecords = Math.Clamp(ReadInteger(section, "MaxRecords", 5000), 1, 50000),
                ReadFromBeginning = ReadBoolean(section, "ReadFromBeginning", defaultValue: true)
            };
        }

        private static string ReadString(IConfiguration section, string key, string defaultValue)
        {
            return string.IsNullOrWhiteSpace(section[key]) ? defaultValue : section[key]!;
        }

        private static int ReadInteger(IConfiguration section, string key, int defaultValue)
        {
            return int.TryParse(section[key], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                ? value
                : defaultValue;
        }

        private static bool ReadBoolean(IConfiguration section, string key, bool defaultValue)
        {
            return bool.TryParse(section[key], out var value) ? value : defaultValue;
        }
    }
}
