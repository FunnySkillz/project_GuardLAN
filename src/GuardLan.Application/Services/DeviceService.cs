using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;

namespace GuardLan.Application.Services;

public sealed class DeviceService(IGuardLanRepository repository) : IDeviceService
{
    public async Task<IReadOnlyList<DeviceDto>> ListAsync(CancellationToken cancellationToken)
    {
        var devices = await repository.ListDevicesAsync(cancellationToken);

        return devices.Select(DeviceDto.FromEntity).ToArray();
    }

    public async Task<DeviceDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var device = await repository.GetDeviceAsync(id, cancellationToken);

        return device is null ? null : DeviceDto.FromEntity(device);
    }

    public async Task<DeviceDto?> UpdateAsync(Guid id, UpdateDeviceCommand command, CancellationToken cancellationToken)
    {
        var device = await repository.GetDeviceAsync(id, cancellationToken);

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

        await repository.SaveChangesAsync(cancellationToken);

        return DeviceDto.FromEntity(device);
    }
}
