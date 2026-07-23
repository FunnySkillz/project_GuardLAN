using GuardLan.Domain.Entities;

namespace GuardLan.Domain.Repositories;

public interface IMdacRegistrationRepository
{
    Task<MdacRegistration?> GetByDeviceIdAsync(Guid deviceId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MdacRegistration>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(MdacRegistration registration, CancellationToken cancellationToken = default);
}
