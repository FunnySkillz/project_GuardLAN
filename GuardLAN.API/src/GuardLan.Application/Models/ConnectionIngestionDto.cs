namespace GuardLan.Application.Models;

public sealed class ConnectionIngestionBatchDto
{
    public string? Source { get; init; }

    public IReadOnlyList<ConnectionIngestionRecordDto> Records { get; init; } =
        [];
}

public sealed class ConnectionIngestionRecordDto
{
    public string? SourceRecordId { get; init; }

    public string SourceIp { get; init; } = string.Empty;

    public string DestinationIp { get; init; } = string.Empty;

    public string? DestinationDomain { get; init; }

    public string Protocol { get; init; } = string.Empty;

    public int? DestinationPort { get; init; }

    public long BytesSent { get; init; }

    public long BytesReceived { get; init; }

    public DateTime StartedUtc { get; init; }

    public DateTime EndedUtc { get; init; }
}

public sealed record ConnectionIngestionResultDto(
    string Source,
    int RecordsRead,
    int Imported,
    int SkippedDuplicates,
    int SkippedInvalid,
    int SkippedUnmatchedDevices,
    int MatchedDevices,
    DateTime ImportedAtUtc,
    string Message);
