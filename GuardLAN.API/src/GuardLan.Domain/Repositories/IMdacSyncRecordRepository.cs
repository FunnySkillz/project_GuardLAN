using GuardLan.Domain.Entities;

namespace GuardLan.Domain.Repositories;

public interface IMdacSyncRecordRepository
{
    Task<IReadOnlyList<MdacSyncRecord>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MdacSyncRecord>> GetByDeviceIdAsync(Guid deviceId, CancellationToken cancellationToken = default);

    Task AddAsync(MdacSyncRecord syncRecord, CancellationToken cancellationToken = default);
}
