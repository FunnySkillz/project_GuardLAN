using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using GuardLan.Application.Zeek;
using GuardLan.Domain.Enums;

namespace GuardLan.Application.Services;

public sealed class ZeekDnsImportService(
    IZeekDnsSource zeekDnsSource,
    IDnsRecordIngestionService dnsRecordIngestionService,
    IIntegrationHealthService integrationHealthService,
    TimeProvider timeProvider) : IZeekDnsImportService
{
    public async Task<DnsIngestionResultDto> ImportRecentAsync(CancellationToken cancellationToken = default)
    {
        var importedAtUtc = timeProvider.GetUtcNow().UtcDateTime;

        if (!zeekDnsSource.IsEnabled)
        {
            var disabledResult = new DnsIngestionResultDto(
                zeekDnsSource.SourceName,
                SourceEnabled: false,
                RecordsRead: 0,
                Imported: 0,
                SkippedDuplicates: 0,
                SkippedInvalid: 0,
                MatchedDevices: 0,
                importedAtUtc,
                $"{zeekDnsSource.SourceName} ingestion is disabled.");

            await RecordHealthAsync(disabledResult, sourceAvailable: false, cancellationToken);

            return disabledResult;
        }

        var sourceResult = await ReadSourceAsync(importedAtUtc, cancellationToken);
        if (!sourceResult.SourceAvailable)
        {
            var unavailableResult = new DnsIngestionResultDto(
                zeekDnsSource.SourceName,
                SourceEnabled: true,
                sourceResult.LinesRead,
                Imported: 0,
                SkippedDuplicates: 0,
                sourceResult.SkippedInvalid,
                MatchedDevices: 0,
                importedAtUtc,
                sourceResult.Message);

            await RecordHealthAsync(unavailableResult, sourceAvailable: false, cancellationToken);

            return unavailableResult;
        }

        var ingestionResult = await dnsRecordIngestionService.ImportAsync(
            zeekDnsSource.SourceName,
            sourceEnabled: true,
            sourceResult.Records,
            cancellationToken);

        if (sourceResult.Checkpoint is not null)
        {
            await zeekDnsSource.SaveCheckpointAsync(sourceResult.Checkpoint, cancellationToken);
        }

        var result = ingestionResult with
        {
            RecordsRead = sourceResult.LinesRead,
            SkippedInvalid = ingestionResult.SkippedInvalid + sourceResult.SkippedInvalid
        };

        await RecordHealthAsync(result, sourceAvailable: true, cancellationToken);

        return result;
    }

    private async Task<ZeekDnsReadResult> ReadSourceAsync(
        DateTime importedAtUtc,
        CancellationToken cancellationToken)
    {
        try
        {
            return await zeekDnsSource.ReadNewQueriesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await RecordHealthAsync(
                new DnsIngestionResultDto(
                    zeekDnsSource.SourceName,
                    SourceEnabled: true,
                    RecordsRead: 0,
                    Imported: 0,
                    SkippedDuplicates: 0,
                    SkippedInvalid: 0,
                    MatchedDevices: 0,
                    importedAtUtc,
                    exception.Message),
                sourceAvailable: false,
                cancellationToken);

            throw;
        }
    }

    private Task RecordHealthAsync(
        DnsIngestionResultDto result,
        bool sourceAvailable,
        CancellationToken cancellationToken)
    {
        return integrationHealthService.RecordAsync(
            new IntegrationHealthRecord(
                result.Source,
                IntegrationKind.Zeek,
                result.SourceEnabled,
                sourceAvailable,
                result.RecordsRead,
                result.Imported,
                result.SkippedInvalid,
                result.ImportedAtUtc,
                result.Message),
            cancellationToken);
    }
}
