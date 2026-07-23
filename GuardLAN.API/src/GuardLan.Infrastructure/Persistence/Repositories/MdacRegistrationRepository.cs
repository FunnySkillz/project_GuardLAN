using GuardLan.Domain.Entities;
using GuardLan.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GuardLan.Infrastructure.Persistence.Repositories;

public sealed class MdacRegistrationRepository(GuardLanDbContext dbContext) : IMdacRegistrationRepository
{
    private readonly GuardLanDbContext _dbContext = dbContext;

    public Task<MdacRegistration?> GetByDeviceIdAsync(Guid deviceId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<MdacRegistration>()
            .AsNoTracking()
            .FirstOrDefaultAsync(registration => registration.DeviceId == deviceId, cancellationToken);
    }

    public async Task<IReadOnlyList<MdacRegistration>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<MdacRegistration>()
            .AsNoTracking()
            .OrderByDescending(registration => registration.RegisteredUtc)
            .ToArrayAsync(cancellationToken);
    }

    public async Task AddAsync(MdacRegistration registration, CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<MdacRegistration>().AddAsync(registration, cancellationToken);
    }
}
