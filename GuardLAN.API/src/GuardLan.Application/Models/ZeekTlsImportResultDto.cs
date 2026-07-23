namespace GuardLan.Application.Models;

public sealed record ZeekTlsImportResultDto(
    string Source,
    bool SourceEnabled,
    bool SourceAvailable,
    int LinesRead,
    int RecordsParsed,
    int SkippedInvalidSourceRecords,
    int Imported,
    int SkippedDuplicates,
    int SkippedInvalidTlsRecords,
    int SkippedUnmatchedDevices,
    int MatchedDevices,
    int MatchedConnections,
    DateTime ImportedAtUtc,
    string Message);
