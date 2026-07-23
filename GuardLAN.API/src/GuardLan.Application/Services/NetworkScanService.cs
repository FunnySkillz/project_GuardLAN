using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using GuardLan.Domain.Entities;
using GuardLan.Domain.Enums;
using GuardLan.Domain.Repositories;

namespace GuardLan.Application.Services;

public sealed class NetworkScanService(IUnitOfWork unitOfWork, TimeProvider timeProvider) : INetworkScanService
{
    private const string DefaultSubnet = "192.168.1.0/24";

    public async Task<IReadOnlyList<NetworkScanDto>> ListAsync(CancellationToken cancellationToken)
    {
        var scanRuns = await unitOfWork.NetworkScanRuns.GetRecentAsync(cancellationToken);

        return scanRuns.Select(NetworkScanDto.FromEntity).ToArray();
    }

    public async Task<NetworkScanDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var scanRun = await unitOfWork.NetworkScanRuns.GetByIdAsync(id, cancellationToken);

        return scanRun is null ? null : NetworkScanDto.FromEntity(scanRun);
    }

    public async Task<NetworkScanDto> QueueAsync(QueueNetworkScanCommand command, CancellationToken cancellationToken)
    {
        var subnet = string.IsNullOrWhiteSpace(command.Subnet) ? DefaultSubnet : command.Subnet.Trim();

        var scanRun = new NetworkScanRun
        {
            Id = Guid.NewGuid(),
            Subnet = subnet,
            Status = NetworkScanStatus.Queued,
            RequestedUtc = timeProvider.GetUtcNow().UtcDateTime,
            Notes = "Queued for the scanner worker."
        };

        await unitOfWork.NetworkScanRuns.AddAsync(scanRun, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return NetworkScanDto.FromEntity(scanRun);
    }
}
