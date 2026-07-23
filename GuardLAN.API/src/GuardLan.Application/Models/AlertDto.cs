using GuardLan.Domain.Entities;
using GuardLan.Domain.Enums;

namespace GuardLan.Application.Models;

public sealed record AlertDto(
    Guid Id,
    Guid? DeviceId,
    AlertSeverity Severity,
    string Type,
    string Message,
    DateTime CreatedUtc,
    DateTime? ResolvedUtc)
{
    public static AlertDto FromEntity(SecurityAlert alert)
    {
        return new AlertDto(
            alert.Id,
            alert.DeviceId,
            alert.Severity,
            alert.Type,
            alert.Message,
            alert.CreatedUtc,
            alert.ResolvedUtc);
    }
}
