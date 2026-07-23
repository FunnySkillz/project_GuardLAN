using GuardLan.Application.Models;

namespace GuardLan.Application.Abstractions;

public interface INetworkScanService
{
    Task<NetworkScanDto> QueueAsync(QueueNetworkScanCommand command, CancellationToken cancellationToken);
}
