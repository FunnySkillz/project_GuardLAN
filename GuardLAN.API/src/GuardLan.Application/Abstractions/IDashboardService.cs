using GuardLan.Application.Models;

namespace GuardLan.Application.Abstractions;

public interface IDashboardService
{
    Task<DashboardSnapshotDto> GetSnapshotAsync(CancellationToken cancellationToken);

    Task<DashboardOverviewDto> GetOverviewAsync(CancellationToken cancellationToken);
}
