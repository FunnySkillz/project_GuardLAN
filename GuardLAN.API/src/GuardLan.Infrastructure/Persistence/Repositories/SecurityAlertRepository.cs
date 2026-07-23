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
            .Include(alert => alert.Device)
            .OrderByDescending(alert => alert.CreatedUtc)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SecurityAlert>> GetOpenAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(alert => alert.Device)
            .Where(alert => alert.ResolvedUtc == null)
            .OrderByDescending(alert => alert.CreatedUtc)
            .ToArrayAsync(cancellationToken);
    }

    public Task<SecurityAlert?> GetByIdWithDeviceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return DbSet
            .Include(alert => alert.Device)
            .FirstOrDefaultAsync(alert => alert.Id == id, cancellationToken);
    }
}
