using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;

namespace GuardLan.Application.Services;

public sealed class AlertService(IGuardLanRepository repository, TimeProvider timeProvider) : IAlertService
{
    public async Task<IReadOnlyList<AlertDto>> ListAsync(CancellationToken cancellationToken)
    {
        var alerts = await repository.ListAlertsAsync(cancellationToken);

        return alerts.Select(AlertDto.FromEntity).ToArray();
    }

    public async Task<AlertDto?> ResolveAsync(Guid id, CancellationToken cancellationToken)
    {
        var alert = await repository.GetAlertAsync(id, cancellationToken);

        if (alert is null)
        {
            return null;
        }

        alert.ResolvedUtc = timeProvider.GetUtcNow().UtcDateTime;
        await repository.SaveChangesAsync(cancellationToken);

        return AlertDto.FromEntity(alert);
    }
}
