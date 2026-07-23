using GuardLan.Application.Dns;
using GuardLan.Application.Models;

namespace GuardLan.Application.Abstractions;

public interface IDnsRecordIngestionService
{
    Task<DnsIngestionResultDto> ImportAsync(
        string sourceName,
        bool sourceEnabled,
        IReadOnlyList<DnsIngestionRecord> sourceRecords,
        CancellationToken cancellationToken = default);
}
