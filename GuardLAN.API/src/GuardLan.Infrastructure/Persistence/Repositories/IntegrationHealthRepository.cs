using GuardLan.Domain.Entities;
using GuardLan.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GuardLan.Infrastructure.Persistence.Repositories;

public sealed class IntegrationHealthRepository(GuardLanDbContext dbContext)
    : GenericRepository<IntegrationHealth>(dbContext),
      IIntegrationHealthRepository
{
    public async Task<IReadOnlyList<IntegrationHealth>> GetAllOrderedAsync(
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .OrderBy(health => health.Kind)
            .ThenBy(health => health.Source)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IntegrationHealth?> GetBySourceAsync(
        string source,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(health => health.Source == source, cancellationToken);
    }
}
