using GuardLan.Domain.Enums;

namespace GuardLan.Domain.Entities;

public class SecurityAlert
{
    public Guid Id { get; set; }

    public Guid? DeviceId { get; set; }

    public NetworkDevice? Device { get; set; }

    public Guid? ConnectionId { get; set; }

    public NetworkConnection? Connection { get; set; }

    public string? Source { get; set; }

    public string? SourceRecordId { get; set; }

    public string? SourceIp { get; set; }

    public string? DestinationIp { get; set; }

    public int? DestinationPort { get; set; }

    public string? Protocol { get; set; }

    public AlertSeverity Severity { get; set; }

    public AlertReviewStatus ReviewStatus { get; set; }

    public string Type { get; set; } = null!;

    public string Message { get; set; } = null!;

    public DateTime CreatedUtc { get; set; }

    public DateTime? ReviewedUtc { get; set; }

    public DateTime? ResolvedUtc { get; set; }

    public string? ReviewNote { get; set; }

    public string? EvidenceSummary { get; set; }

    public ICollection<SecurityAlertHistory> History { get; } = [];
}
