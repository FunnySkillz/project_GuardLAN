using GuardLan.Domain.Entities;
using GuardLan.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GuardLan.Infrastructure.Persistence.Repositories;

public sealed class DeviceRepository(GuardLanDbContext dbContext)
    : GenericRepository<NetworkDevice>(dbContext),
      IDeviceRepository
{
    public async Task<IReadOnlyList<NetworkDevice>> GetInventoryAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .OrderByDescending(device => device.IsOnline)
            .ThenBy(device => device.IpAddress)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NetworkDevice>> GetDevicesForScanUpdateAsync(
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .OrderBy(device => device.IpAddress)
            .ToArrayAsync(cancellationToken);
    }

    public Task<NetworkDevice?> GetByMacAddressAsync(string macAddress, CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(device => device.MacAddress == macAddress, cancellationToken);
    }

    public async Task<IReadOnlyList<NetworkDevice>> GetOnlineDevicesAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(device => device.IsOnline)
            .OrderBy(device => device.Hostname ?? device.IpAddress)
            .ToArrayAsync(cancellationToken);
    }

    public Task<bool> ExistsByMacAddressAsync(string macAddress, CancellationToken cancellationToken = default)
    {
        return DbSet.AnyAsync(device => device.MacAddress == macAddress, cancellationToken);
    }
}
