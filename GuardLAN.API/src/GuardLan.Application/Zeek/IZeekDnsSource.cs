using GuardLan.Application.Dns;

namespace GuardLan.Application.Zeek;

public interface IZeekDnsSource
{
    bool IsEnabled { get; }

    string SourceName { get; }

    Task<ZeekDnsReadResult> ReadNewQueriesAsync(
        CancellationToken cancellationToken = default);

    Task SaveCheckpointAsync(
        ZeekLogCheckpoint checkpoint,
        CancellationToken cancellationToken = default);
}

public sealed record ZeekDnsReadResult(
    bool SourceAvailable,
    int LinesRead,
    int RecordsParsed,
    int SkippedInvalid,
    IReadOnlyList<DnsIngestionRecord> Records,
    ZeekLogCheckpoint? Checkpoint,
    string Message);
