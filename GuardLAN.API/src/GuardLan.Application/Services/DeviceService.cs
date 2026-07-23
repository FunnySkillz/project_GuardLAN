using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using GuardLan.Domain.Repositories;

namespace GuardLan.Application.Services;

public sealed class DeviceService(IUnitOfWork unitOfWork) : IDeviceService
{
    public async Task<IReadOnlyList<DeviceDto>> ListAsync(CancellationToken cancellationToken)
    {
        var devices = await unitOfWork.Devices.GetInventoryAsync(cancellationToken);

        return devices.Select(DeviceDto.FromEntity).ToArray();
    }

    public async Task<DeviceDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var device = await unitOfWork.Devices.GetByIdAsync(id, cancellationToken);

        return device is null ? null : DeviceDto.FromEntity(device);
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

        return DeviceDto.FromEntity(device);
    }
}
