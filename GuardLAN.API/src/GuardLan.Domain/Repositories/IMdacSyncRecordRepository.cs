using GuardLan.Domain.Entities;

namespace GuardLan.Domain.Repositories;

public interface IMdacSyncRecordRepository
{
    Task AddAsync(MdacSyncRecord syncRecord, CancellationToken cancellationToken = default);
}
