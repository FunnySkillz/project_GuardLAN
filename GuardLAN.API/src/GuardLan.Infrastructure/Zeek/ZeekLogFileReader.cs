using GuardLan.Application.Zeek;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GuardLan.Infrastructure.Zeek;

public sealed class ZeekLogFileReader(
    TimeProvider timeProvider,
    ILogger<ZeekLogFileReader> logger)
{
    private static readonly JsonSerializerOptions CheckpointJsonOptions = new()
    {
        WriteIndented = true
    };

    public async Task<ZeekLogReadResult<TRecord>> ReadNewRowsAsync<TRecord>(
        ZeekLogFileOptions options,
        string sourceName,
        ZeekLogFieldSet defaultFields,
        Func<ZeekLogRow, TRecord?> parseRecord,
        CancellationToken cancellationToken = default)
        where TRecord : class
    {
        var sourcePath = NormalizeConfiguredPath(options.Path);
        if (sourcePath.Length == 0)
        {
            return Unavailable<TRecord>($"{sourceName} path is not configured.");
        }

        if (!File.Exists(sourcePath))
        {
            return Unavailable<TRecord>($"{sourceName} was not found at {sourcePath}.");
        }

        var checkpoint = await LoadCheckpointAsync(options, sourcePath, cancellationToken);
        if (!options.ReadFromBeginning && checkpoint is null)
        {
            var lineCount = await CountLinesAsync(sourcePath, cancellationToken);

            return Available(
                LinesRead: 0,
                RecordsParsed: 0,
                SkippedInvalid: 0,
                Records: Array.Empty<TRecord>(),
                Checkpoint: CreateCheckpoint(sourcePath, lineCount),
                Message: $"Checkpointed existing {sourceName} content at line {lineCount}.");
        }

        var startLine = checkpoint?.LineNumber ?? 0;
        var readAttempt = await ReadFromLineAsync(
            sourcePath,
            startLine,
            options.MaxRecords,
            defaultFields,
            parseRecord,
            cancellationToken);

        if (readAttempt.TotalLines < startLine)
        {
            readAttempt = await ReadFromLineAsync(
                sourcePath,
                startLine: 0,
                options.MaxRecords,
                defaultFields,
                parseRecord,
                cancellationToken);
        }

        var nextLine = readAttempt.LastProcessedLine > 0
            ? readAttempt.LastProcessedLine
            : readAttempt.TotalLines;
        var message = readAttempt.LinesRead == 0
            ? $"No new {sourceName} rows were found."
            : $"Read {readAttempt.LinesRead} {sourceName} rows and parsed {readAttempt.RecordsParsed} records.";

        return Available(
            readAttempt.LinesRead,
            readAttempt.RecordsParsed,
            readAttempt.SkippedInvalid,
            readAttempt.Records,
            CreateCheckpoint(sourcePath, nextLine),
            message);
    }

    public async Task SaveCheckpointAsync(
        ZeekLogFileOptions options,
        ZeekLogCheckpoint checkpoint,
        CancellationToken cancellationToken = default)
    {
        var checkpointPath = ResolveCheckpointPath(options, checkpoint.SourcePath);
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

    private async Task<ReadAttempt<TRecord>> ReadFromLineAsync<TRecord>(
        string sourcePath,
        int startLine,
        int maxRecords,
        ZeekLogFieldSet defaultFields,
        Func<ZeekLogRow, TRecord?> parseRecord,
        CancellationToken cancellationToken)
        where TRecord : class
    {
        var fields = defaultFields;
        var records = new List<TRecord>();
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

            if (linesRead >= maxRecords)
            {
                break;
            }

            linesRead++;
            lastProcessedLine = currentLine;

            var record = parseRecord(new ZeekLogRow(Split(line), fields));
            if (record is null)
            {
                skippedInvalid++;
                continue;
            }

            records.Add(record);
        }

        return new ReadAttempt<TRecord>(
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

    private async Task<ZeekLogCheckpoint?> LoadCheckpointAsync(
        ZeekLogFileOptions options,
        string sourcePath,
        CancellationToken cancellationToken)
    {
        var checkpointPath = ResolveCheckpointPath(options, sourcePath);
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
            var checkpoint = await JsonSerializer.DeserializeAsync<ZeekLogCheckpoint>(
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
            logger.LogWarning(exception, "Could not read Zeek checkpoint {CheckpointPath}.", checkpointPath);
            return null;
        }
        catch (IOException exception)
        {
            logger.LogWarning(exception, "Could not read Zeek checkpoint {CheckpointPath}.", checkpointPath);
            return null;
        }
    }

    private ZeekLogCheckpoint CreateCheckpoint(string sourcePath, int lineNumber)
    {
        return new ZeekLogCheckpoint(
            sourcePath,
            Math.Max(0, lineNumber),
            timeProvider.GetUtcNow().UtcDateTime);
    }

    private static bool TryParseFields(string line, out ZeekLogFieldSet fields)
    {
        var values = Split(line);
        if (values.Length < 2 || !string.Equals(values[0], "#fields", StringComparison.Ordinal))
        {
            fields = ZeekLogFieldSet.Empty;
            return false;
        }

        fields = ZeekLogFieldSet.FromFieldNames(values.Skip(1));
        return true;
    }

    private static string[] Split(string line)
    {
        return line.Split('\t');
    }

    private static string ResolveCheckpointPath(ZeekLogFileOptions options, string sourcePath)
    {
        var configuredPath = NormalizeConfiguredPath(options.CheckpointPath);

        return configuredPath.Length == 0
            ? $"{sourcePath}.guardlan-checkpoint.json"
            : configuredPath;
    }

    private static string NormalizeConfiguredPath(string? configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return string.Empty;
        }

        return Path.GetFullPath(configuredPath.Trim());
    }

    private static ZeekLogReadResult<TRecord> Unavailable<TRecord>(string message)
    {
        return new ZeekLogReadResult<TRecord>(
            SourceAvailable: false,
            LinesRead: 0,
            RecordsParsed: 0,
            SkippedInvalid: 0,
            Records: Array.Empty<TRecord>(),
            Checkpoint: null,
            Message: message);
    }

    private static ZeekLogReadResult<TRecord> Available<TRecord>(
        int LinesRead,
        int RecordsParsed,
        int SkippedInvalid,
        IReadOnlyList<TRecord> Records,
        ZeekLogCheckpoint? Checkpoint,
        string Message)
    {
        return new ZeekLogReadResult<TRecord>(
            SourceAvailable: true,
            LinesRead: LinesRead,
            RecordsParsed: RecordsParsed,
            SkippedInvalid: SkippedInvalid,
            Records: Records,
            Checkpoint: Checkpoint,
            Message: Message);
    }

    private sealed record ReadAttempt<TRecord>(
        int TotalLines,
        int LastProcessedLine,
        int LinesRead,
        int RecordsParsed,
        int SkippedInvalid,
        IReadOnlyList<TRecord> Records);
}

public sealed record ZeekLogReadResult<TRecord>(
    bool SourceAvailable,
    int LinesRead,
    int RecordsParsed,
    int SkippedInvalid,
    IReadOnlyList<TRecord> Records,
    ZeekLogCheckpoint? Checkpoint,
    string Message);

public sealed class ZeekLogFieldSet
{
    public static ZeekLogFieldSet Empty { get; } = new(new Dictionary<string, int>(StringComparer.Ordinal));

    private readonly IReadOnlyDictionary<string, int> indexes;

    private ZeekLogFieldSet(IReadOnlyDictionary<string, int> indexes)
    {
        this.indexes = indexes;
    }

    public static ZeekLogFieldSet FromFieldNames(IEnumerable<string> fieldNames)
    {
        return new ZeekLogFieldSet(
            fieldNames
                .Select((fieldName, index) => new { FieldName = fieldName, Index = index })
                .ToDictionary(field => field.FieldName, field => field.Index, StringComparer.Ordinal));
    }

    public int IndexOf(string fieldName)
    {
        return indexes.TryGetValue(fieldName, out var index) ? index : -1;
    }
}

public readonly struct ZeekLogRow(string[] values, ZeekLogFieldSet fields)
{
    public string Read(string fieldName)
    {
        return Read(fields.IndexOf(fieldName));
    }

    public string Read(string fieldName, int fallbackIndex)
    {
        var value = Read(fieldName);

        return value.Length == 0 ? Read(fallbackIndex) : value;
    }

    private string Read(int index)
    {
        if (index < 0 || index >= values.Length)
        {
            return string.Empty;
        }

        var value = values[index].Trim();

        return value is "-" or "(empty)" ? string.Empty : value;
    }
}
