using GuardLan.Domain.Entities;

namespace GuardLan.Application.Abstractions;

public interface IGuardLanRepository
{
    Task<IReadOnlyList<NetworkDevice>> ListDevicesAsync(CancellationToken cancellationToken);

    Task<NetworkDevice?> GetDeviceAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<SecurityAlert>> ListAlertsAsync(CancellationToken cancellationToken);

    Task<SecurityAlert?> GetAlertAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<DnsQuery>> ListDnsQueriesSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken);

    Task<IReadOnlyList<NetworkConnection>> ListConnectionsSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken);

    Task<NetworkScanRun> AddScanRunAsync(NetworkScanRun scanRun, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
