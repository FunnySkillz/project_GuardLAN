namespace GuardLan.Domain.Entities;

public class DnsQuery
{
    public Guid Id { get; set; }

    public Guid? DeviceId { get; set; }

    public NetworkDevice? Device { get; set; }

    public string ClientIp { get; set; } = null!;

    public string Domain { get; set; } = null!;

    public bool WasBlocked { get; set; }

    public DateTime TimestampUtc { get; set; }
}
