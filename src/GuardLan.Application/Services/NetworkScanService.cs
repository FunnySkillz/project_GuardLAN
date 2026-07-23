using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using GuardLan.Domain.Entities;
using GuardLan.Domain.Enums;

namespace GuardLan.Application.Services;

public sealed class NetworkScanService(IGuardLanRepository repository, TimeProvider timeProvider) : INetworkScanService
{
    private const string DefaultSubnet = "192.168.1.0/24";

    public async Task<NetworkScanDto> QueueAsync(QueueNetworkScanCommand command, CancellationToken cancellationToken)
    {
        var subnet = string.IsNullOrWhiteSpace(command.Subnet) ? DefaultSubnet : command.Subnet.Trim();

        var scanRun = new NetworkScanRun
        {
            Id = Guid.NewGuid(),
            Subnet = subnet,
            Status = NetworkScanStatus.Queued,
            RequestedUtc = timeProvider.GetUtcNow().UtcDateTime,
            Notes = "Queued for the scanner worker. nmap execution will be added in the next implementation step."
        };

        await repository.AddScanRunAsync(scanRun, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return NetworkScanDto.FromEntity(scanRun);
    }
}
