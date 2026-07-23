using GuardLan.Application.Models;
using GuardLan.Application.Zeek;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json;

namespace GuardLan.Infrastructure.Zeek;

public sealed class ZeekConnLogSource : IZeekConnectionSource
{
    private static readonly JsonSerializerOptions CheckpointJsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly ZeekConnLogOptions options;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<ZeekConnLogSource> logger;

    public ZeekConnLogSource(
        IConfiguration configuration,
        TimeProvider timeProvider,
        ILogger<ZeekConnLogSource> logger)
    {
        options = ZeekConnLogOptions.FromConfiguration(configuration);
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public bool IsEnabled => options.Enabled;

    public string SourceName => "Zeek conn.log";

    public async Task<ZeekConnectionReadResult> ReadNewConnectionsAsync(
        CancellationToken cancellationToken = default)
    {
        var sourcePath = NormalizeConfiguredPath(options.Path);
        if (sourcePath.Length == 0)
        {
            return Unavailable("Zeek conn.log path is not configured.");
        }

        if (!File.Exists(sourcePath))
        {
            return Unavailable($"Zeek conn.log was not found at {sourcePath}.");
        }

        if (!options.ReadFromBeginning &&
            await LoadCheckpointAsync(sourcePath, cancellationToken) is null)
        {
            var lineCount = await CountLinesAsync(sourcePath, cancellationToken);

            return Available(
                LinesRead: 0,
                RecordsParsed: 0,
                SkippedInvalid: 0,
                Records: [],
                Checkpoint: CreateCheckpoint(sourcePath, lineCount),
                $"Checkpointed existing Zeek conn.log content at line {lineCount}.");
        }

        var checkpoint = await LoadCheckpointAsync(sourcePath, cancellationToken);
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
            ? "No new Zeek conn.log rows were found."
            : $"Read {readAttempt.LinesRead} Zeek conn.log rows and parsed {readAttempt.RecordsParsed} connections.";

        return Available(
            readAttempt.LinesRead,
            readAttempt.RecordsParsed,
            readAttempt.SkippedInvalid,
            readAttempt.Records,
            CreateCheckpoint(sourcePath, nextLine),
            message);
    }

    public async Task SaveCheckpointAsync(
        ZeekConnectionCheckpoint checkpoint,
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
        var fields = ZeekConnFields.Default;
        var records = new List<ConnectionIngestionRecordDto>();
        var currentLine = 0;
        var lastProcessedLine = 0;
        var linesRead = 0;
        var skippedInvalid = 0;

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

            if (TryParseFields(line, out var parsedFields))
            {
                fields = parsedFields;
                continue;
            }

            if (currentLine <= startLine ||
                line.Length == 0 ||
                line.StartsWith('#'))
            {
                continue;
            }

            if (linesRead >= options.MaxRecords)
            {
                break;
            }

            linesRead++;
            lastProcessedLine = currentLine;

            if (TryParseConnection(line, fields, out var record))
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

    private async Task<ZeekConnectionCheckpoint?> LoadCheckpointAsync(
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
            var checkpoint = await JsonSerializer.DeserializeAsync<ZeekConnectionCheckpoint>(
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
            logger.LogWarning(exception, "Could not read Zeek conn.log checkpoint {CheckpointPath}.", checkpointPath);
            return null;
        }
        catch (IOException exception)
        {
            logger.LogWarning(exception, "Could not read Zeek conn.log checkpoint {CheckpointPath}.", checkpointPath);
            return null;
        }
    }

    private ZeekConnectionCheckpoint CreateCheckpoint(string sourcePath, int lineNumber)
    {
        return new ZeekConnectionCheckpoint(
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

    private static bool TryParseFields(string line, out ZeekConnFields fields)
    {
        var values = Split(line);
        if (values.Length < 2 || !string.Equals(values[0], "#fields", StringComparison.Ordinal))
        {
            fields = ZeekConnFields.Default;
            return false;
        }

        var indexes = values
            .Skip(1)
            .Select((name, index) => new { Name = name, Index = index })
            .ToDictionary(field => field.Name, field => field.Index, StringComparer.Ordinal);

        fields = new ZeekConnFields(
            ReadIndex(indexes, "ts"),
            ReadIndex(indexes, "id.orig_h"),
            ReadIndex(indexes, "id.resp_h"),
            ReadIndex(indexes, "id.resp_p"),
            ReadIndex(indexes, "proto"),
            ReadIndex(indexes, "duration"),
            ReadIndex(indexes, "orig_bytes"),
            ReadIndex(indexes, "resp_bytes"));

        return fields.HasRequiredFields;
    }

    private static bool TryParseConnection(
        string line,
        ZeekConnFields fields,
        out ConnectionIngestionRecordDto record)
    {
        var values = Split(line);
        var sourceIp = ReadField(values, fields.SourceIp);
        var destinationIp = ReadField(values, fields.DestinationIp);
        var protocol = ReadField(values, fields.Protocol);

        if (sourceIp.Length == 0 ||
            destinationIp.Length == 0 ||
            protocol.Length == 0 ||
            !TryParseTimestamp(ReadField(values, fields.Timestamp), out var startedUtc) ||
            !TryParsePort(ReadField(values, fields.DestinationPort), out var destinationPort))
        {
            record = EmptyRecord();
            return false;
        }

        var durationSeconds = ParseNullableDouble(ReadField(values, fields.Duration));
        var endedUtc = durationSeconds is >= 0
            ? startedUtc.AddSeconds(durationSeconds.Value)
            : startedUtc;

        record = new ConnectionIngestionRecordDto
        {
            SourceIp = sourceIp,
            DestinationIp = destinationIp,
            DestinationDomain = null,
            Protocol = protocol,
            DestinationPort = destinationPort,
            BytesSent = ParseNullableLong(ReadField(values, fields.BytesSent)) ?? 0,
            BytesReceived = ParseNullableLong(ReadField(values, fields.BytesReceived)) ?? 0,
            StartedUtc = startedUtc,
            EndedUtc = endedUtc
        };

        return true;
    }

    private static string[] Split(string line)
    {
        return line.Split('\t');
    }

    private static string ReadField(string[] values, int index)
    {
        if (index < 0 || index >= values.Length)
        {
            return string.Empty;
        }

        var value = values[index].Trim();

        return value is "-" or "(empty)" ? string.Empty : value;
    }

    private static int ReadIndex(Dictionary<string, int> indexes, string fieldName)
    {
        return indexes.TryGetValue(fieldName, out var index) ? index : -1;
    }

    private static bool TryParseTimestamp(string value, out DateTime timestampUtc)
    {
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) &&
            seconds > 0)
        {
            try
            {
                timestampUtc = DateTime.UnixEpoch.AddSeconds(seconds);
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
            }
        }

        timestampUtc = default;
        return false;
    }

    private static bool TryParsePort(string value, out int? port)
    {
        if (value.Length == 0)
        {
            port = null;
            return true;
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) &&
            parsed is >= 0 and <= 65535)
        {
            port = parsed;
            return true;
        }

        port = null;
        return false;
    }

    private static double? ParseNullableDouble(string value)
    {
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static long? ParseNullableLong(string value)
    {
        return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) &&
               parsed >= 0
            ? parsed
            : null;
    }

    private static ConnectionIngestionRecordDto EmptyRecord()
    {
        return new ConnectionIngestionRecordDto
        {
            SourceIp = string.Empty,
            DestinationIp = string.Empty,
            Protocol = string.Empty,
            StartedUtc = DateTime.UnixEpoch,
            EndedUtc = DateTime.UnixEpoch
        };
    }

    private static string NormalizeConfiguredPath(string? configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return string.Empty;
        }

        return Path.GetFullPath(configuredPath.Trim());
    }

    private static ZeekConnectionReadResult Unavailable(string message)
    {
        return new ZeekConnectionReadResult(
            SourceAvailable: false,
            LinesRead: 0,
            RecordsParsed: 0,
            SkippedInvalid: 0,
            Records: [],
            Checkpoint: null,
            Message: message);
    }

    private static ZeekConnectionReadResult Available(
        int LinesRead,
        int RecordsParsed,
        int SkippedInvalid,
        IReadOnlyList<ConnectionIngestionRecordDto> Records,
        ZeekConnectionCheckpoint? Checkpoint,
        string Message)
    {
        return new ZeekConnectionReadResult(
            SourceAvailable: true,
            LinesRead: LinesRead,
            RecordsParsed: RecordsParsed,
            SkippedInvalid: SkippedInvalid,
            Records: Records,
            Checkpoint: Checkpoint,
            Message: Message);
    }

    private sealed record ReadAttempt(
        int TotalLines,
        int LastProcessedLine,
        int LinesRead,
        int RecordsParsed,
        int SkippedInvalid,
        IReadOnlyList<ConnectionIngestionRecordDto> Records);

    private sealed record ZeekConnFields(
        int Timestamp,
        int SourceIp,
        int DestinationIp,
        int DestinationPort,
        int Protocol,
        int Duration,
        int BytesSent,
        int BytesReceived)
    {
        public static ZeekConnFields Default { get; } = new(
            Timestamp: 0,
            SourceIp: 2,
            DestinationIp: 4,
            DestinationPort: 5,
            Protocol: 6,
            Duration: 8,
            BytesSent: 9,
            BytesReceived: 10);

        public bool HasRequiredFields =>
            Timestamp >= 0 &&
            SourceIp >= 0 &&
            DestinationIp >= 0 &&
            DestinationPort >= 0 &&
            Protocol >= 0;
    }

    private sealed class ZeekConnLogOptions
    {
        public bool Enabled { get; init; }

        public string Path { get; init; } = string.Empty;

        public string CheckpointPath { get; init; } = string.Empty;

        public int MaxRecords { get; init; } = 5000;

        public bool ReadFromBeginning { get; init; } = true;

        public static ZeekConnLogOptions FromConfiguration(IConfiguration configuration)
        {
            var section = configuration.GetSection("Zeek:ConnLog");

            return new ZeekConnLogOptions
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
