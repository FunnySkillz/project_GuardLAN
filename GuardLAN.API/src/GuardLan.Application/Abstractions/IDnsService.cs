using GuardLan.Application.Models;

namespace GuardLan.Application.Abstractions;

public interface IDnsService
{
    Task<DnsOverviewDto> GetOverviewAsync(
        int hours,
        int limit,
        CancellationToken cancellationToken);
}
