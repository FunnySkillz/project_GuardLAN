using GuardLan.Application.Abstractions;
using GuardLan.Application.Dns;
using GuardLan.Application.Models;

namespace GuardLan.Application.Services;

public sealed class DnsIngestionService(
    IDnsQuerySource dnsQuerySource,
    IDnsRecordIngestionService dnsRecordIngestionService) : IDnsIngestionService
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

        var sourceRecords = await dnsQuerySource.GetRecentQueriesAsync(cancellationToken);

        return await dnsRecordIngestionService.ImportAsync(
            dnsQuerySource.SourceName,
            sourceEnabled: true,
            sourceRecords,
            cancellationToken);
    }
}
