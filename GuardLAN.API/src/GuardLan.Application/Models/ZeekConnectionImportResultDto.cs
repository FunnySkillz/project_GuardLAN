namespace GuardLan.Application.Models;

public sealed record ZeekConnectionImportResultDto(
    string Source,
    bool SourceEnabled,
    bool SourceAvailable,
    int LinesRead,
    int RecordsParsed,
    int SkippedInvalidSourceRecords,
    int Imported,
    int SkippedDuplicates,
    int SkippedInvalidConnectionRecords,
    int SkippedUnmatchedDevices,
    int MatchedDevices,
    DateTime ImportedAtUtc,
    string Message);
