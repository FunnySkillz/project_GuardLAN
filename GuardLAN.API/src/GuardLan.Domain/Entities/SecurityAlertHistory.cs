namespace GuardLan.Domain.Entities;

public class SecurityAlertHistory
{
    public Guid Id { get; set; }

    public Guid SecurityAlertId { get; set; }

    public SecurityAlert? SecurityAlert { get; set; }

    public string EventType { get; set; } = null!;

    public string Description { get; set; } = null!;

    public DateTime CreatedUtc { get; set; }
}
