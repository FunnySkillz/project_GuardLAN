using GuardLan.Application.Models;

namespace GuardLan.Application.Abstractions;

public interface IConnectionIngestionService
{
    Task<ConnectionIngestionResultDto> ImportAsync(
        ConnectionIngestionBatchDto batch,
        CancellationToken cancellationToken = default);
}
