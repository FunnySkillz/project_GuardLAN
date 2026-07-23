namespace GuardLan.Infrastructure.Zeek;

public sealed record ZeekLogFileOptions(
    bool Enabled,
    string Path,
    string CheckpointPath,
    int MaxRecords,
    bool ReadFromBeginning);
