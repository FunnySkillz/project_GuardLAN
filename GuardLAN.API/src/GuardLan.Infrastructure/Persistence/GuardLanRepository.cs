using GuardLan.Application.Abstractions;
using GuardLan.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GuardLan.Infrastructure.Persistence;

public sealed class GuardLanRepository(GuardLanDbContext dbContext) : IGuardLanRepository
{
    public async Task<IReadOnlyList<NetworkDevice>> ListDevicesAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Devices
            .AsNoTracking()
            .OrderByDescending(device => device.IsOnline)
            .ThenBy(device => device.IpAddress)
            .ToArrayAsync(cancellationToken);
    }

    public Task<NetworkDevice?> GetDeviceAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Devices.FirstOrDefaultAsync(device => device.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<SecurityAlert>> ListAlertsAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Alerts
            .AsNoTracking()
            .OrderByDescending(alert => alert.CreatedUtc)
            .ToArrayAsync(cancellationToken);
    }

    public Task<SecurityAlert?> GetAlertAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Alerts.FirstOrDefaultAsync(alert => alert.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<DnsQuery>> ListDnsQueriesSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken)
    {
        return await dbContext.DnsQueries
            .AsNoTracking()
            .Where(query => query.TimestampUtc >= sinceUtc)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NetworkConnection>> ListConnectionsSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken)
    {
        return await dbContext.Connections
            .AsNoTracking()
            .Where(connection => connection.LastSeenUtc >= sinceUtc)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<NetworkScanRun> AddScanRunAsync(NetworkScanRun scanRun, CancellationToken cancellationToken)
    {
        await dbContext.NetworkScanRuns.AddAsync(scanRun, cancellationToken);

        return scanRun;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
