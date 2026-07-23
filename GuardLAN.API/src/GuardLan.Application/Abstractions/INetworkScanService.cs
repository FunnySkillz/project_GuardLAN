using GuardLan.Application.Models;

namespace GuardLan.Application.Abstractions;

public interface INetworkScanService
{
    Task<IReadOnlyList<NetworkScanDto>> ListAsync(CancellationToken cancellationToken);

    Task<NetworkScanDto?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<NetworkScanDto> QueueAsync(QueueNetworkScanCommand command, CancellationToken cancellationToken);
}
