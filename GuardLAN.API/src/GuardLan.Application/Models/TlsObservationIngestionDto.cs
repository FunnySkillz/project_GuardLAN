namespace GuardLan.Application.Models;

public sealed class TlsObservationIngestionBatchDto
{
    public string? Source { get; init; }

    public IReadOnlyList<TlsObservationIngestionRecordDto> Records { get; init; } = [];
}

public sealed class TlsObservationIngestionRecordDto
{
    public string? SourceRecordId { get; init; }

    public string SourceIp { get; init; } = string.Empty;

    public string DestinationIp { get; init; } = string.Empty;

    public int? DestinationPort { get; init; }

    public string? ServerName { get; init; }

    public string? Version { get; init; }

    public string? Cipher { get; init; }

    public string? Ja3 { get; init; }

    public string? Ja3s { get; init; }

    public string? Alpn { get; init; }

    public bool? WasEstablished { get; init; }

    public DateTime ObservedUtc { get; init; }
}

public sealed record TlsObservationIngestionResultDto(
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
