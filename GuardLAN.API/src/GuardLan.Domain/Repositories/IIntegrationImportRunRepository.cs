using GuardLan.Domain.Entities;

namespace GuardLan.Domain.Repositories;

public interface IIntegrationImportRunRepository : IGenericRepository<IntegrationImportRun>
{
    Task<IReadOnlyList<IntegrationImportRun>> GetRecentAsync(
        int limit,
        CancellationToken cancellationToken = default);

    Task EnsureSchemaAsync(CancellationToken cancellationToken = default);
}
