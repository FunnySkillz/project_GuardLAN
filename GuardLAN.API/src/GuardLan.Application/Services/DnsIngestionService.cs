using GuardLan.Application.Abstractions;
using GuardLan.Application.Dns;
using GuardLan.Application.Models;
using GuardLan.Domain.Enums;

namespace GuardLan.Application.Services;

public sealed class DnsIngestionService(
    IDnsQuerySource dnsQuerySource,
    IDnsRecordIngestionService dnsRecordIngestionService,
    IIntegrationHealthService integrationHealthService,
    TimeProvider timeProvider) : IDnsIngestionService
{
    public async Task<DnsIngestionResultDto> ImportRecentAsync(CancellationToken cancellationToken = default)
    {
        if (!dnsQuerySource.IsEnabled)
        {
            return await dnsRecordIngestionService.ImportAsync(
                dnsQuerySource.SourceName,
                sourceEnabled: false,
                sourceRecords: [],
                cancellationToken);
        }

        IReadOnlyList<DnsIngestionRecord> sourceRecords;
        try
        {
            sourceRecords = await dnsQuerySource.GetRecentQueriesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await integrationHealthService.RecordAsync(
                new IntegrationHealthRecord(
                    dnsQuerySource.SourceName,
                    IntegrationKind.Dns,
                    SourceEnabled: true,
                    SourceAvailable: false,
                    RecordsRead: 0,
                    RecordsImported: 0,
                    RecordsRejected: 0,
                    timeProvider.GetUtcNow().UtcDateTime,
                    exception.Message),
                cancellationToken);

            throw;
        }

        return await dnsRecordIngestionService.ImportAsync(
            dnsQuerySource.SourceName,
            sourceEnabled: true,
            sourceRecords,
            cancellationToken);
    }
}
