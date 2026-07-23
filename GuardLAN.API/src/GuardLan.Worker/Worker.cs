using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
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
        var zeekIngestionInterval = TimeSpan.FromSeconds(
            Math.Max(30, configuration.GetValue("Zeek:ImportIntervalSeconds", 300)));
        var suricataIngestionInterval = TimeSpan.FromSeconds(
            Math.Max(30, configuration.GetValue("Suricata:ImportIntervalSeconds", 300)));
        var lastDnsIngestionUtc = DateTimeOffset.MinValue;
        var lastZeekIngestionUtc = DateTimeOffset.MinValue;
        var lastSuricataIngestionUtc = DateTimeOffset.MinValue;

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
                    await RunDnsIngestionAsync(scope, stoppingToken);
                }

                if (now - lastZeekIngestionUtc >= zeekIngestionInterval)
                {
                    lastZeekIngestionUtc = now;
                    await RunZeekIngestionAsync(scope, stoppingToken);
                }

                if (now - lastSuricataIngestionUtc >= suricataIngestionInterval)
                {
                    lastSuricataIngestionUtc = now;
                    await RunSuricataIngestionAsync(scope, stoppingToken);
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

    private async Task RunDnsIngestionAsync(IServiceScope scope, CancellationToken stoppingToken)
    {
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

    private async Task RunZeekIngestionAsync(IServiceScope scope, CancellationToken stoppingToken)
    {
        try
        {
            var zeekImportService = scope.ServiceProvider.GetRequiredService<IZeekImportService>();
            var zeekResult = await zeekImportService.ImportRecentAsync(stoppingToken);

            LogZeekConnectionResult(zeekResult.Connections);
            LogZeekDnsResult(zeekResult.Dns);
            LogZeekTlsResult(zeekResult.Tls);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "GuardLAN Zeek ingestion failed.");
        }
    }

    private void LogZeekConnectionResult(ZeekConnectionImportResultDto result)
    {
        if (!result.SourceEnabled)
        {
            logger.LogDebug(
                "Zeek connection ingestion source {Source} is disabled.",
                result.Source);
        }
        else if (!result.SourceAvailable)
        {
            logger.LogWarning(
                "Zeek connection ingestion source {Source} is unavailable: {Message}",
                result.Source,
                result.Message);
        }
        else
        {
            logger.LogInformation(
                "Zeek connection ingestion from {Source}: imported {Imported}, skipped {SkippedDuplicates} duplicates, skipped {SkippedInvalid} invalid records.",
                result.Source,
                result.Imported,
                result.SkippedDuplicates,
                result.SkippedInvalidSourceRecords +
                result.SkippedInvalidConnectionRecords);
        }
    }

    private async Task RunSuricataIngestionAsync(IServiceScope scope, CancellationToken stoppingToken)
    {
        try
        {
            var suricataAlertImportService =
                scope.ServiceProvider.GetRequiredService<ISuricataAlertImportService>();
            var result = await suricataAlertImportService.ImportRecentAsync(stoppingToken);

            if (!result.SourceEnabled)
            {
                logger.LogDebug("Suricata ingestion source {Source} is disabled.", result.Source);
            }
            else if (!result.SourceAvailable)
            {
                logger.LogWarning(
                    "Suricata ingestion source {Source} is unavailable: {Message}",
                    result.Source,
                    result.Message);
            }
            else
            {
                logger.LogInformation(
                    "Suricata ingestion from {Source}: imported {Imported}, matched {MatchedConnections} connections, skipped {SkippedDuplicates} duplicates, skipped {SkippedInvalid} invalid records.",
                    result.Source,
                    result.Imported,
                    result.MatchedConnections,
                    result.SkippedDuplicates,
                    result.SkippedInvalidSourceRecords +
                    result.SkippedInvalidAlertRecords);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "GuardLAN Suricata ingestion failed.");
        }
    }

    private void LogZeekDnsResult(DnsIngestionResultDto result)
    {
        if (!result.SourceEnabled)
        {
            logger.LogDebug("Zeek DNS ingestion source {Source} is disabled.", result.Source);
            return;
        }

        logger.LogInformation(
            "Zeek DNS ingestion from {Source}: imported {Imported}, skipped {SkippedDuplicates} duplicates, skipped {SkippedInvalid} invalid records.",
            result.Source,
            result.Imported,
            result.SkippedDuplicates,
            result.SkippedInvalid);
    }

    private void LogZeekTlsResult(ZeekTlsImportResultDto result)
    {
        if (!result.SourceEnabled)
        {
            logger.LogDebug("Zeek TLS ingestion source {Source} is disabled.", result.Source);
        }
        else if (!result.SourceAvailable)
        {
            logger.LogWarning(
                "Zeek TLS ingestion source {Source} is unavailable: {Message}",
                result.Source,
                result.Message);
        }
        else
        {
            logger.LogInformation(
                "Zeek TLS ingestion from {Source}: imported {Imported}, matched {MatchedConnections} connections, skipped {SkippedDuplicates} duplicates, skipped {SkippedInvalid} invalid records.",
                result.Source,
                result.Imported,
                result.MatchedConnections,
                result.SkippedDuplicates,
                result.SkippedInvalidSourceRecords +
                result.SkippedInvalidTlsRecords);
        }
    }
}
