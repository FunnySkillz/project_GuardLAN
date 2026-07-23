using GuardLan.Domain.Enums;

namespace GuardLan.Domain.Entities;

public class SecurityAlert
{
    public Guid Id { get; set; }

    public Guid? DeviceId { get; set; }

    public NetworkDevice? Device { get; set; }

    public AlertSeverity Severity { get; set; }

    public string Type { get; set; } = null!;

    public string Message { get; set; } = null!;

    public DateTime CreatedUtc { get; set; }

    public DateTime? ResolvedUtc { get; set; }
}
