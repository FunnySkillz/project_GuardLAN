using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using GuardLan.Domain.Entities;
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
        var alert = await unitOfWork.SecurityAlerts.GetByIdWithDeviceAsync(id, cancellationToken);

        if (alert is null)
        {
            return null;
        }

        var resolvedUtc = timeProvider.GetUtcNow().UtcDateTime;
        alert.ResolvedUtc = resolvedUtc;
        alert.History.Add(
            new SecurityAlertHistory
            {
                Id = Guid.NewGuid(),
                SecurityAlertId = alert.Id,
                EventType = "Resolved",
                Description = "Alert was marked resolved.",
                CreatedUtc = resolvedUtc
            });
        unitOfWork.SecurityAlerts.Update(alert);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return AlertDto.FromEntity(alert);
    }
}
