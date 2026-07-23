using GuardLan.Domain.Entities;

namespace GuardLan.Domain.Repositories;

public interface IDeviceRepository : IGenericRepository<NetworkDevice>
{
    Task<IReadOnlyList<NetworkDevice>> GetInventoryAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NetworkDevice>> GetDevicesForScanUpdateAsync(CancellationToken cancellationToken = default);

    Task<NetworkDevice?> GetByMacAddressAsync(string macAddress, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NetworkDevice>> GetOnlineDevicesAsync(CancellationToken cancellationToken = default);

    Task<bool> ExistsByMacAddressAsync(string macAddress, CancellationToken cancellationToken = default);
}
