using GuardLan.Application.Models;

namespace GuardLan.Application.Suricata;

public interface ISuricataAlertSource
{
    bool IsEnabled { get; }

    string SourceName { get; }

    Task<SuricataAlertReadResult> ReadNewAlertsAsync(
        CancellationToken cancellationToken = default);

    Task SaveCheckpointAsync(
        SuricataLogCheckpoint checkpoint,
        CancellationToken cancellationToken = default);
}

public sealed record SuricataAlertReadResult(
    bool SourceAvailable,
    int LinesRead,
    int RecordsParsed,
    int SkippedInvalid,
    IReadOnlyList<IdsAlertIngestionRecordDto> Records,
    SuricataLogCheckpoint? Checkpoint,
    string Message);

public sealed record SuricataLogCheckpoint(
    string SourcePath,
    int LineNumber,
    DateTime CheckedAtUtc);
