using GuardLan.Domain.Entities;
using GuardLan.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GuardLan.Infrastructure.Persistence.Repositories;

public sealed class TlsObservationRepository(GuardLanDbContext dbContext)
    : GenericRepository<TlsObservation>(dbContext),
      ITlsObservationRepository
{
    public async Task<IReadOnlyList<TlsObservation>> GetSinceAsync(
        DateTime sinceUtc,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(observation => observation.ObservedUtc >= sinceUtc)
            .ToArrayAsync(cancellationToken);
    }
}
