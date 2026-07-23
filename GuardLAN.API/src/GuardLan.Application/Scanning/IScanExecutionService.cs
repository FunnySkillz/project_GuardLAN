namespace GuardLan.Application.Scanning;

public interface IScanExecutionService
{
    Task<ScanExecutionResult> ExecuteNextQueuedScanAsync(CancellationToken cancellationToken = default);
}
