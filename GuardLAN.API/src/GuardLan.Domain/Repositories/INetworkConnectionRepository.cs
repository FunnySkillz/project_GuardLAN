using GuardLan.Domain.Entities;

namespace GuardLan.Domain.Repositories;

public interface INetworkConnectionRepository : IGenericRepository<NetworkConnection>
{
    Task<IReadOnlyList<NetworkConnection>> GetSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken = default);
}
