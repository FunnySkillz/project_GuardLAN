using GuardLan.Domain.Entities;

namespace GuardLan.Domain.Repositories;

public interface ISecurityAlertRepository : IGenericRepository<SecurityAlert>
{
    Task<IReadOnlyList<SecurityAlert>> GetRecentAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SecurityAlert>> GetOpenAsync(CancellationToken cancellationToken = default);

    Task<SecurityAlert?> GetByIdWithDeviceAsync(Guid id, CancellationToken cancellationToken = default);
}
