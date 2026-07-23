using GuardLan.Application.Models;

namespace GuardLan.Application.Abstractions;

public interface IIntegrationHealthService
{
    Task<IntegrationHealthOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default);

    Task RecordAsync(IntegrationHealthRecord record, CancellationToken cancellationToken = default);
}
