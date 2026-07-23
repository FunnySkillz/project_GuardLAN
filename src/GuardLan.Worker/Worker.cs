namespace GuardLan.Worker;

public class Worker(ILogger<Worker> logger) : BackgroundService
{
    private static readonly TimeSpan ScannerInterval = TimeSpan.FromSeconds(60);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation(
                "GuardLAN scanner heartbeat at {Time}. nmap execution will be wired into this loop next.",
                DateTimeOffset.UtcNow);

            await Task.Delay(ScannerInterval, stoppingToken);
        }
    }
}
