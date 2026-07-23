using GuardLan.Domain.Entities;
using GuardLan.Domain.Enums;
using GuardLan.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GuardLan.Infrastructure.Persistence.Repositories;

public sealed class NetworkScanRunRepository(GuardLanDbContext dbContext)
    : GenericRepository<NetworkScanRun>(dbContext),
      INetworkScanRunRepository
{
    public async Task<IReadOnlyList<NetworkScanRun>> GetRecentAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .OrderByDescending(scanRun => scanRun.RequestedUtc)
            .Take(50)
            .ToArrayAsync(cancellationToken);
    }

    public Task<NetworkScanRun?> GetNextQueuedAsync(CancellationToken cancellationToken = default)
    {
        return DbSet
            .Where(scanRun => scanRun.Status == NetworkScanStatus.Queued)
            .OrderBy(scanRun => scanRun.RequestedUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
