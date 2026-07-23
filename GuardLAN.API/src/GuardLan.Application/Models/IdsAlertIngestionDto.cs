namespace GuardLan.Application.Models;

public sealed class IdsAlertIngestionBatchDto
{
    public string? Source { get; init; }

    public IReadOnlyList<IdsAlertIngestionRecordDto> Records { get; init; } = [];
}

public sealed class IdsAlertIngestionRecordDto
{
    public string? SourceRecordId { get; init; }

    public string? SourceIp { get; init; }

    public string? DestinationIp { get; init; }

    public int? SourcePort { get; init; }

    public int? DestinationPort { get; init; }

    public string? Protocol { get; init; }

    public string Signature { get; init; } = string.Empty;

    public string? Category { get; init; }

    public int? Severity { get; init; }

    public string? Action { get; init; }

    public string? EvidenceSummary { get; init; }

    public DateTime TimestampUtc { get; init; }
}

public sealed record IdsAlertIngestionResultDto(
    string Source,
    int RecordsRead,
    int Imported,
    int SkippedDuplicates,
    int SkippedInvalid,
    int SkippedUnmatchedDevices,
    int MatchedDevices,
    int MatchedConnections,
    DateTime ImportedAtUtc,
    string Message);
