using GuardLan.Domain.Entities;
using GuardLan.Domain.Repositories;

namespace GuardLan.Infrastructure.Persistence.Repositories;

public sealed class MdacSyncRecordRepository(GuardLanDbContext dbContext) : IMdacSyncRecordRepository
{
    private readonly GuardLanDbContext _dbContext = dbContext;

    public async Task AddAsync(MdacSyncRecord syncRecord, CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<MdacSyncRecord>().AddAsync(syncRecord, cancellationToken);
    }
}
