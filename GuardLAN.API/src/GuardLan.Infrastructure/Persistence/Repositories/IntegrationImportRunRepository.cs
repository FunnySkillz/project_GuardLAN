using GuardLan.Domain.Entities;
using GuardLan.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GuardLan.Infrastructure.Persistence.Repositories;

public sealed class IntegrationImportRunRepository(GuardLanDbContext dbContext)
    : GenericRepository<IntegrationImportRun>(dbContext),
      IIntegrationImportRunRepository
{
    public async Task<IReadOnlyList<IntegrationImportRun>> GetRecentAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .OrderByDescending(run => run.CompletedUtc)
            .Take(Math.Clamp(limit, 1, 100))
            .ToArrayAsync(cancellationToken);
    }
}
