using GuardLan.Application.Models;

namespace GuardLan.Application.Abstractions;

public interface IZeekDnsImportService
{
    Task<DnsIngestionResultDto> ImportRecentAsync(
        CancellationToken cancellationToken = default);
}
