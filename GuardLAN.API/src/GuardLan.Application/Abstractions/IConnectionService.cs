using GuardLan.Application.Models;

namespace GuardLan.Application.Abstractions;

public interface IConnectionService
{
    Task<ConnectionOverviewDto> GetOverviewAsync(
        ConnectionOverviewQueryDto query,
        CancellationToken cancellationToken = default);
}
