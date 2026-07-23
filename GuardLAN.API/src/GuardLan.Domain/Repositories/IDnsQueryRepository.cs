using GuardLan.Domain.Entities;

namespace GuardLan.Domain.Repositories;

public interface IDnsQueryRepository : IGenericRepository<DnsQuery>
{
    Task<IReadOnlyList<DnsQuery>> GetSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DnsQuery>> GetSinceWithDevicesAsync(
        DateTime sinceUtc,
        CancellationToken cancellationToken = default);
}
