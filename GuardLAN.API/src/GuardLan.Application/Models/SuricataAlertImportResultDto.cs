namespace GuardLan.Application.Models;

public sealed record SuricataAlertImportResultDto(
    string Source,
    bool SourceEnabled,
    bool SourceAvailable,
    int LinesRead,
    int RecordsParsed,
    int SkippedInvalidSourceRecords,
    int Imported,
    int SkippedDuplicates,
    int SkippedInvalidAlertRecords,
    int SkippedUnmatchedDevices,
    int MatchedDevices,
    int MatchedConnections,
    DateTime ImportedAtUtc,
    string Message);
