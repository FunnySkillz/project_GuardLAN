using GuardLan.Application.Models;

namespace GuardLan.Application.Zeek;

public interface IZeekConnectionSource
{
    bool IsEnabled { get; }

    string SourceName { get; }

    Task<ZeekConnectionReadResult> ReadNewConnectionsAsync(
        CancellationToken cancellationToken = default);

    Task SaveCheckpointAsync(
        ZeekConnectionCheckpoint checkpoint,
        CancellationToken cancellationToken = default);
}

public sealed record ZeekConnectionReadResult(
    bool SourceAvailable,
    int LinesRead,
    int RecordsParsed,
    int SkippedInvalid,
    IReadOnlyList<ConnectionIngestionRecordDto> Records,
    ZeekConnectionCheckpoint? Checkpoint,
    string Message);

public sealed record ZeekConnectionCheckpoint(
    string SourcePath,
    int LineNumber,
    DateTime CheckedAtUtc);
