using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using GuardLan.Application.Zeek;

namespace GuardLan.Application.Services;

public sealed class ZeekDnsImportService(
    IZeekDnsSource zeekDnsSource,
    IDnsRecordIngestionService dnsRecordIngestionService) : IZeekDnsImportService
{
    public async Task<DnsIngestionResultDto> ImportRecentAsync(CancellationToken cancellationToken = default)
    {
        if (!zeekDnsSource.IsEnabled)
        {
            return await dnsRecordIngestionService.ImportAsync(
                zeekDnsSource.SourceName,
                sourceEnabled: false,
                sourceRecords: [],
                cancellationToken);
        }

        var sourceResult = await zeekDnsSource.ReadNewQueriesAsync(cancellationToken);
        if (!sourceResult.SourceAvailable)
        {
            return new DnsIngestionResultDto(
                zeekDnsSource.SourceName,
                SourceEnabled: true,
                sourceResult.LinesRead,
                Imported: 0,
                SkippedDuplicates: 0,
                sourceResult.SkippedInvalid,
                MatchedDevices: 0,
                DateTime.UtcNow,
                sourceResult.Message);
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

        return ingestionResult with
        {
            RecordsRead = sourceResult.LinesRead,
            SkippedInvalid = ingestionResult.SkippedInvalid + sourceResult.SkippedInvalid
        };
    }
}
