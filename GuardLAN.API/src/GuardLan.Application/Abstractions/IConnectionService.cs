using GuardLan.Application.Models;

namespace GuardLan.Application.Abstractions;

public interface IConnectionService
{
    Task<ConnectionOverviewDto> GetOverviewAsync(
        int hours,
        int limit,
        CancellationToken cancellationToken = default);
}
