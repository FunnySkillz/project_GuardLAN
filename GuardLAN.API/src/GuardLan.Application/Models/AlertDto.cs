using GuardLan.Domain.Entities;
using GuardLan.Domain.Enums;

namespace GuardLan.Application.Models;

public sealed record AlertDto(
    Guid Id,
    Guid? DeviceId,
    string? DeviceName,
    string? DeviceIpAddress,
    string? DeviceMacAddress,
    Guid? ConnectionId,
    string? Source,
    string? SourceRecordId,
    string? SourceIp,
    string? DestinationIp,
    int? DestinationPort,
    string? Protocol,
    AlertSeverity Severity,
    AlertReviewStatus ReviewStatus,
    string Type,
    string Message,
    DateTime CreatedUtc,
    DateTime? ReviewedUtc,
    DateTime? ResolvedUtc,
    string? ReviewNote,
    string? EvidenceSummary,
    IReadOnlyList<AlertHistoryDto> History)
{
    public static AlertDto FromEntity(SecurityAlert alert)
    {
        return new AlertDto(
            alert.Id,
            alert.DeviceId,
            alert.Device?.Hostname ?? alert.Device?.IpAddress,
            alert.Device?.IpAddress,
            alert.Device?.MacAddress,
            alert.ConnectionId,
            alert.Source,
            alert.SourceRecordId,
            alert.SourceIp,
            alert.DestinationIp,
            alert.DestinationPort,
            alert.Protocol,
            alert.Severity,
            alert.ReviewStatus,
            alert.Type,
            alert.Message,
            alert.CreatedUtc,
            alert.ReviewedUtc,
            alert.ResolvedUtc,
            alert.ReviewNote,
            alert.EvidenceSummary,
            alert.History
                .OrderByDescending(history => history.CreatedUtc)
                .Select(AlertHistoryDto.FromEntity)
                .ToArray());
    }
}

public sealed record AlertReviewCommand(string? Note);

public sealed record AlertHistoryDto(
    Guid Id,
    string EventType,
    string Description,
    DateTime CreatedUtc)
{
    public static AlertHistoryDto FromEntity(SecurityAlertHistory history)
    {
        return new AlertHistoryDto(
            history.Id,
            history.EventType,
            history.Description,
            history.CreatedUtc);
    }
}
