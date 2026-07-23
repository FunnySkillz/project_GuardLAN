using GuardLan.Application.Models;

namespace GuardLan.Application.Abstractions;

public interface IDeviceService
{
    Task<IReadOnlyList<DeviceDto>> ListAsync(CancellationToken cancellationToken);

    Task<DeviceDto?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<DeviceEvidenceDto?> GetEvidenceAsync(Guid id, CancellationToken cancellationToken);

    Task<DeviceDto?> UpdateAsync(Guid id, UpdateDeviceCommand command, CancellationToken cancellationToken);
}
