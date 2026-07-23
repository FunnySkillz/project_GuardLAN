using GuardLan.Domain.Entities;
using GuardLan.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GuardLan.Infrastructure.Persistence.Repositories;

public sealed class MdacSyncRecordRepository(GuardLanDbContext dbContext) : IMdacSyncRecordRepository
{
    private readonly GuardLanDbContext _dbContext = dbContext;

    public async Task<IReadOnlyList<MdacSyncRecord>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<MdacSyncRecord>()
            .AsNoTracking()
            .OrderByDescending(syncRecord => syncRecord.SyncedUtc)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MdacSyncRecord>> GetByDeviceIdAsync(Guid deviceId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<MdacSyncRecord>()
            .AsNoTracking()
            .Where(syncRecord => syncRecord.DeviceId == deviceId)
            .OrderByDescending(syncRecord => syncRecord.SyncedUtc)
            .ToArrayAsync(cancellationToken);
    }

    public async Task AddAsync(MdacSyncRecord syncRecord, CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<MdacSyncRecord>().AddAsync(syncRecord, cancellationToken);
    }
}
