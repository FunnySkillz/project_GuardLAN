using GuardLan.Domain.Entities;

namespace GuardLan.Domain.Repositories;

public interface ITlsObservationRepository : IGenericRepository<TlsObservation>
{
    Task<IReadOnlyList<TlsObservation>> GetSinceAsync(
        DateTime sinceUtc,
        CancellationToken cancellationToken = default);
}
