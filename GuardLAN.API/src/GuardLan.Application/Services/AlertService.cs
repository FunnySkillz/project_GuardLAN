using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using GuardLan.Domain.Entities;
using GuardLan.Domain.Enums;
using GuardLan.Domain.Repositories;

namespace GuardLan.Application.Services;

public sealed class AlertService(
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILiveUpdatePublisher liveUpdatePublisher) : IAlertService
{
    public async Task<IReadOnlyList<AlertDto>> ListAsync(CancellationToken cancellationToken)
    {
        var alerts = await unitOfWork.SecurityAlerts.GetRecentAsync(cancellationToken);

        return alerts.Select(AlertDto.FromEntity).ToArray();
    }

    public Task<AlertDto?> MarkReviewedAsync(
        Guid id,
        AlertReviewCommand command,
        CancellationToken cancellationToken)
    {
        return SetReviewStatusAsync(
            id,
            AlertReviewStatus.Reviewed,
            command,
            "Reviewed",
            "Alert was marked reviewed.",
            cancellationToken);
    }

    public Task<AlertDto?> ResolveAsync(
        Guid id,
        AlertReviewCommand command,
        CancellationToken cancellationToken)
    {
        return SetReviewStatusAsync(
            id,
            AlertReviewStatus.Resolved,
            command,
            "Resolved",
            "Alert was marked resolved.",
            cancellationToken);
    }

    public Task<AlertDto?> MarkFalsePositiveAsync(
        Guid id,
        AlertReviewCommand command,
        CancellationToken cancellationToken)
    {
        return SetReviewStatusAsync(
            id,
            AlertReviewStatus.FalsePositive,
            command,
            "FalsePositive",
            "Alert was marked as a false positive.",
            cancellationToken);
    }

    public Task<AlertDto?> SuppressAsync(
        Guid id,
        AlertReviewCommand command,
        CancellationToken cancellationToken)
    {
        return SetReviewStatusAsync(
            id,
            AlertReviewStatus.Suppressed,
            command,
            "Suppressed",
            "Alert was suppressed.",
            cancellationToken);
    }

    public Task<AlertDto?> ReopenAsync(
        Guid id,
        AlertReviewCommand command,
        CancellationToken cancellationToken)
    {
        return SetReviewStatusAsync(
            id,
            AlertReviewStatus.Open,
            command,
            "Reopened",
            "Alert was reopened.",
            cancellationToken);
    }

    private async Task<AlertDto?> SetReviewStatusAsync(
        Guid id,
        AlertReviewStatus status,
        AlertReviewCommand command,
        string historyEventType,
        string historyDescription,
        CancellationToken cancellationToken)
    {
        var alert = await unitOfWork.SecurityAlerts.GetByIdWithDeviceAsync(id, cancellationToken);

        if (alert is null)
        {
            return null;
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var note = NormalizeNote(command.Note);

        alert.ReviewStatus = status;
        alert.ReviewedUtc = nowUtc;
        alert.ReviewNote = note;
        alert.ResolvedUtc = IsClosedStatus(status) ? nowUtc : null;
        alert.History.Add(
            new SecurityAlertHistory
            {
                Id = Guid.NewGuid(),
                SecurityAlertId = alert.Id,
                EventType = historyEventType,
                Description = BuildHistoryDescription(historyDescription, note),
                CreatedUtc = nowUtc
            });
        unitOfWork.SecurityAlerts.Update(alert);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await liveUpdatePublisher.PublishAsync(
            new LiveUpdateDto(
                status == AlertReviewStatus.Resolved
                    ? LiveUpdateTypes.AlertResolved
                    : LiveUpdateTypes.AlertUpdated,
                $"{historyDescription} {alert.Message}",
                nowUtc,
                DeviceId: alert.DeviceId,
                AlertId: alert.Id,
                Status: status.ToString()),
            cancellationToken);

        return AlertDto.FromEntity(alert);
    }

    private static bool IsClosedStatus(AlertReviewStatus status)
    {
        return status is
            AlertReviewStatus.Resolved or
            AlertReviewStatus.FalsePositive or
            AlertReviewStatus.Suppressed;
    }

    private static string BuildHistoryDescription(string description, string? note)
    {
        var value = string.IsNullOrWhiteSpace(note)
            ? description
            : $"{description} Note: {note}";

        return Truncate(value, 512);
    }

    private static string? NormalizeNote(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return null;
        }

        var trimmed = note.Trim();

        return Truncate(trimmed, 512);
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
