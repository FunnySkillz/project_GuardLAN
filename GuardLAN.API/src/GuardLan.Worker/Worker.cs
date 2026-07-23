using GuardLan.Application.Scanning;

namespace GuardLan.Worker;

public class Worker(
    IServiceScopeFactory serviceScopeFactory,
    IConfiguration configuration,
    ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var scannerInterval = TimeSpan.FromSeconds(
            Math.Max(5, configuration.GetValue("Scanner:IntervalSeconds", 60)));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceScopeFactory.CreateScope();
                var scanExecutionService = scope.ServiceProvider.GetRequiredService<IScanExecutionService>();
                var result = await scanExecutionService.ExecuteNextQueuedScanAsync(stoppingToken);

                if (result.ScanProcessed)
                {
                    logger.LogInformation(
                        "Processed scan {ScanRunId}: {Message}",
                        result.ScanRunId,
                        result.Message);
                }
                else
                {
                    logger.LogDebug("No queued GuardLAN scan found.");
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "GuardLAN scanner worker failed while processing queued scans.");
            }

            await Task.Delay(scannerInterval, stoppingToken);
        }
    }
}
