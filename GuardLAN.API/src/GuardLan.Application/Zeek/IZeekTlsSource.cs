using GuardLan.Application.Models;

namespace GuardLan.Application.Zeek;

public interface IZeekTlsSource
{
    bool IsEnabled { get; }

    string SourceName { get; }

    Task<ZeekTlsReadResult> ReadNewObservationsAsync(
        CancellationToken cancellationToken = default);

    Task SaveCheckpointAsync(
        ZeekLogCheckpoint checkpoint,
        CancellationToken cancellationToken = default);
}

public sealed record ZeekTlsReadResult(
    bool SourceAvailable,
    int LinesRead,
    int RecordsParsed,
    int SkippedInvalid,
    IReadOnlyList<TlsObservationIngestionRecordDto> Records,
    ZeekLogCheckpoint? Checkpoint,
    string Message);
