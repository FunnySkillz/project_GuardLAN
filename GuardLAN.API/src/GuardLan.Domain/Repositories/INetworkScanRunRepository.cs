using GuardLan.Domain.Entities;

namespace GuardLan.Domain.Repositories;

public interface INetworkScanRunRepository : IGenericRepository<NetworkScanRun>
{
    Task<IReadOnlyList<NetworkScanRun>> GetRecentAsync(CancellationToken cancellationToken = default);

    Task<NetworkScanRun?> GetNextQueuedAsync(CancellationToken cancellationToken = default);
}
