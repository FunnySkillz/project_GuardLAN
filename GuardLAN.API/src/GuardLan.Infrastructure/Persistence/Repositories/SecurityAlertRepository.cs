using GuardLan.Domain.Entities;
using GuardLan.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GuardLan.Infrastructure.Persistence.Repositories;

public sealed class SecurityAlertRepository(GuardLanDbContext dbContext)
    : GenericRepository<SecurityAlert>(dbContext),
      ISecurityAlertRepository
{
    public async Task<IReadOnlyList<SecurityAlert>> GetSinceAsync(
        DateTime sinceUtc,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(alert => alert.Device)
            .Include(alert => alert.Connection)
            .Include(alert => alert.History)
            .Where(alert => alert.CreatedUtc >= sinceUtc)
            .OrderByDescending(alert => alert.CreatedUtc)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SecurityAlert>> GetRecentAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(alert => alert.Device)
            .Include(alert => alert.Connection)
            .Include(alert => alert.History)
            .OrderByDescending(alert => alert.CreatedUtc)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SecurityAlert>> GetOpenAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(alert => alert.Device)
            .Include(alert => alert.Connection)
            .Include(alert => alert.History)
            .Where(alert => alert.ResolvedUtc == null)
            .OrderByDescending(alert => alert.CreatedUtc)
            .ToArrayAsync(cancellationToken);
    }

    public Task<SecurityAlert?> GetByIdWithDeviceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return DbSet
            .Include(alert => alert.Device)
            .Include(alert => alert.Connection)
            .Include(alert => alert.History)
            .FirstOrDefaultAsync(alert => alert.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<SecurityAlert>> GetEvidenceForDeviceAsync(
        Guid deviceId,
        DateTime sinceUtc,
        int limit,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(alert => alert.Device)
            .Include(alert => alert.Connection)
            .Include(alert => alert.History)
            .Where(alert => alert.DeviceId == deviceId && (alert.ResolvedUtc == null || alert.CreatedUtc >= sinceUtc))
            .OrderByDescending(alert => alert.ResolvedUtc == null)
            .ThenByDescending(alert => alert.CreatedUtc)
            .Take(limit)
            .ToArrayAsync(cancellationToken);
    }
}
