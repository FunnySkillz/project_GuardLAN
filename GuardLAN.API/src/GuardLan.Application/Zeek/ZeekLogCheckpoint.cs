namespace GuardLan.Application.Zeek;

public sealed record ZeekLogCheckpoint(
    string SourcePath,
    int LineNumber,
    DateTime CheckedAtUtc);
