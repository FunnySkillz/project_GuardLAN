using GuardLan.Domain.Entities;
using GuardLan.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GuardLan.Infrastructure.Persistence.Repositories;

public sealed class SecurityAlertRepository(GuardLanDbContext dbContext)
    : GenericRepository<SecurityAlert>(dbContext),
      ISecurityAlertRepository
{
    public async Task<IReadOnlyList<SecurityAlert>> GetRecentAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .OrderByDescending(alert => alert.CreatedUtc)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SecurityAlert>> GetOpenAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(alert => alert.ResolvedUtc == null)
            .OrderByDescending(alert => alert.CreatedUtc)
            .ToArrayAsync(cancellationToken);
    }
}
