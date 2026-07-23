namespace GuardLan.Application.Scanning;

public sealed record ScanExecutionResult(
    bool ScanProcessed,
    Guid? ScanRunId,
    int DevicesDiscovered,
    int NewDevices,
    int DevicesMarkedOffline,
    string? Message)
{
    public static ScanExecutionResult NoQueuedScan { get; } =
        new(false, null, 0, 0, 0, "No queued scan run found.");
}
