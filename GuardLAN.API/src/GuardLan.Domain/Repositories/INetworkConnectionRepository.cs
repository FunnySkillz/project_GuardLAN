using GuardLan.Domain.Entities;

namespace GuardLan.Domain.Repositories;

public interface INetworkConnectionRepository : IGenericRepository<NetworkConnection>
{
    Task<IReadOnlyList<NetworkConnection>> GetSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NetworkConnection>> GetSinceWithDevicesAsync(
        DateTime sinceUtc,
        CancellationToken cancellationToken = default);

    Task<NetworkConnectionPage> GetPageSinceWithDevicesAsync(
        NetworkConnectionQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NetworkConnection>> GetRecentForDeviceAsync(
        Guid deviceId,
        DateTime sinceUtc,
        int limit,
        CancellationToken cancellationToken = default);
}
