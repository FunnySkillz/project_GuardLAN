using GuardLan.Application.Models;

namespace GuardLan.Application.Abstractions;

public interface IDnsIngestionService
{
    Task<DnsIngestionResultDto> ImportRecentAsync(CancellationToken cancellationToken = default);
}
