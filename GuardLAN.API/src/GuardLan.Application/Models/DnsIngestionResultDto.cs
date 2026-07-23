namespace GuardLan.Application.Models;

public sealed record DnsIngestionResultDto(
    string Source,
    bool SourceEnabled,
    int RecordsRead,
    int Imported,
    int SkippedDuplicates,
    int SkippedInvalid,
    int MatchedDevices,
    DateTime ImportedAtUtc,
    string Message);
