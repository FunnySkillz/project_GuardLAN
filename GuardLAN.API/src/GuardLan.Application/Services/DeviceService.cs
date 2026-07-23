using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using GuardLan.Domain.Entities;
using GuardLan.Domain.Repositories;

namespace GuardLan.Application.Services;

public sealed class DeviceService(
    IUnitOfWork unitOfWork,
    IDeviceRiskEvaluator deviceRiskEvaluator,
    TimeProvider timeProvider) : IDeviceService
{
    public async Task<IReadOnlyList<DeviceDto>> ListAsync(CancellationToken cancellationToken)
    {
        var devices = await unitOfWork.Devices.GetInventoryAsync(cancellationToken);
        var risks = await LoadDeviceRisksAsync(devices, cancellationToken);

        return devices.Select(device => DeviceDto.FromEntity(device, risks[device.Id])).ToArray();
    }

    public async Task<DeviceDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var device = await unitOfWork.Devices.GetByIdAsync(id, cancellationToken);

        if (device is null)
        {
            return null;
        }

        var risks = await LoadDeviceRisksAsync([device], cancellationToken);

        return DeviceDto.FromEntity(device, risks[device.Id]);
    }

    public async Task<DeviceDto?> UpdateAsync(Guid id, UpdateDeviceCommand command, CancellationToken cancellationToken)
    {
        var device = await unitOfWork.Devices.GetByIdAsync(id, cancellationToken);

        if (device is null)
        {
            return null;
        }

        if (command.Hostname is not null)
        {
            device.Hostname = string.IsNullOrWhiteSpace(command.Hostname) ? null : command.Hostname.Trim();
        }

        if (command.DeviceType.HasValue)
        {
            device.DeviceType = command.DeviceType.Value;
        }

        if (command.IsTrusted.HasValue)
        {
            device.IsTrusted = command.IsTrusted.Value;
        }

        unitOfWork.Devices.Update(device);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var risks = await LoadDeviceRisksAsync([device], cancellationToken);

        return DeviceDto.FromEntity(device, risks[device.Id]);
    }

    private async Task<IReadOnlyDictionary<Guid, DeviceRiskDto>> LoadDeviceRisksAsync(
        IReadOnlyList<NetworkDevice> devices,
        CancellationToken cancellationToken)
    {
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var sinceUtc = nowUtc.AddHours(-24);
        var alerts = await unitOfWork.SecurityAlerts.GetRecentAsync(cancellationToken);
        var dnsQueries = await unitOfWork.DnsQueries.GetSinceAsync(sinceUtc, cancellationToken);
        var connections = await unitOfWork.NetworkConnections.GetSinceAsync(sinceUtc, cancellationToken);

        return deviceRiskEvaluator.Evaluate(devices, alerts, dnsQueries, connections, nowUtc);
    }
}
