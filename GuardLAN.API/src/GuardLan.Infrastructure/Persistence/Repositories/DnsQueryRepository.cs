using GuardLan.Domain.Entities;
using GuardLan.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GuardLan.Infrastructure.Persistence.Repositories;

public sealed class DnsQueryRepository(GuardLanDbContext dbContext)
    : GenericRepository<DnsQuery>(dbContext),
      IDnsQueryRepository
{
    public async Task<IReadOnlyList<DnsQuery>> GetSinceAsync(
        DateTime sinceUtc,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(query => query.TimestampUtc >= sinceUtc)
            .ToArrayAsync(cancellationToken);
    }
}
