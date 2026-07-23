using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using GuardLan.Domain.Repositories;

namespace GuardLan.Application.Services;

public sealed class AlertService(IUnitOfWork unitOfWork, TimeProvider timeProvider) : IAlertService
{
    public async Task<IReadOnlyList<AlertDto>> ListAsync(CancellationToken cancellationToken)
    {
        var alerts = await unitOfWork.SecurityAlerts.GetRecentAsync(cancellationToken);

        return alerts.Select(AlertDto.FromEntity).ToArray();
    }

    public async Task<AlertDto?> ResolveAsync(Guid id, CancellationToken cancellationToken)
    {
        var alert = await unitOfWork.SecurityAlerts.GetByIdAsync(id, cancellationToken);

        if (alert is null)
        {
            return null;
        }

        alert.ResolvedUtc = timeProvider.GetUtcNow().UtcDateTime;
        unitOfWork.SecurityAlerts.Update(alert);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return AlertDto.FromEntity(alert);
    }
}
