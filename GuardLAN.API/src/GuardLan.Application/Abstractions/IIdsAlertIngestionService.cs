using GuardLan.Application.Models;

namespace GuardLan.Application.Abstractions;

public interface IIdsAlertIngestionService
{
    Task<IdsAlertIngestionResultDto> ImportAsync(
        IdsAlertIngestionBatchDto batch,
        CancellationToken cancellationToken = default);
}
