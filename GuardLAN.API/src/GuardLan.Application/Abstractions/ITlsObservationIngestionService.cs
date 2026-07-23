using GuardLan.Application.Models;

namespace GuardLan.Application.Abstractions;

public interface ITlsObservationIngestionService
{
    Task<TlsObservationIngestionResultDto> ImportAsync(
        TlsObservationIngestionBatchDto batch,
        CancellationToken cancellationToken = default);
}
