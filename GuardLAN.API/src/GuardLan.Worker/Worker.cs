using GuardLan.Application.Abstractions;
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
        var dnsIngestionInterval = TimeSpan.FromSeconds(
            Math.Max(30, configuration.GetValue("DnsIngestion:IntervalSeconds", 300)));
        var lastDnsIngestionUtc = DateTimeOffset.MinValue;

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

                var now = DateTimeOffset.UtcNow;
                if (now - lastDnsIngestionUtc >= dnsIngestionInterval)
                {
                    lastDnsIngestionUtc = now;

                    try
                    {
                        var dnsIngestionService = scope.ServiceProvider.GetRequiredService<IDnsIngestionService>();
                        var dnsResult = await dnsIngestionService.ImportRecentAsync(stoppingToken);

                        if (dnsResult.SourceEnabled)
                        {
                            logger.LogInformation(
                                "DNS ingestion from {Source}: imported {Imported}, skipped {SkippedDuplicates} duplicates, skipped {SkippedInvalid} invalid records.",
                                dnsResult.Source,
                                dnsResult.Imported,
                                dnsResult.SkippedDuplicates,
                                dnsResult.SkippedInvalid);
                        }
                        else
                        {
                            logger.LogDebug("DNS ingestion source {Source} is disabled.", dnsResult.Source);
                        }
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        logger.LogError(exception, "GuardLAN DNS ingestion failed.");
                    }
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
