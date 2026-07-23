using GuardLan.Domain.Entities;
using GuardLan.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GuardLan.Infrastructure.Persistence.Repositories;

public sealed class NetworkConnectionRepository(GuardLanDbContext dbContext)
    : GenericRepository<NetworkConnection>(dbContext),
      INetworkConnectionRepository
{
    public async Task<IReadOnlyList<NetworkConnection>> GetSinceAsync(
        DateTime sinceUtc,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(connection => connection.LastSeenUtc >= sinceUtc)
            .ToArrayAsync(cancellationToken);
    }
}
