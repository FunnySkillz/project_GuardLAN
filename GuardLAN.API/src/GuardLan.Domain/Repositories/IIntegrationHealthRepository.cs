using GuardLan.Domain.Entities;

namespace GuardLan.Domain.Repositories;

public interface IIntegrationHealthRepository : IGenericRepository<IntegrationHealth>
{
    Task<IReadOnlyList<IntegrationHealth>> GetAllOrderedAsync(
        CancellationToken cancellationToken = default);

    Task<IntegrationHealth?> GetBySourceAsync(
        string source,
        CancellationToken cancellationToken = default);
}
