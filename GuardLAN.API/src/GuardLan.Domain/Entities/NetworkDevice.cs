using GuardLan.Domain.Enums;

namespace GuardLan.Domain.Entities;

public class NetworkDevice
{
    public Guid Id { get; set; }

    public string IpAddress { get; set; } = null!;

    public string MacAddress { get; set; } = null!;

    public string? Hostname { get; set; }

    public string? Vendor { get; set; }

    public DeviceType DeviceType { get; set; }

    public bool IsTrusted { get; set; }

    public DateTime FirstSeenUtc { get; set; }

    public DateTime LastSeenUtc { get; set; }

    public bool IsOnline { get; set; }

    public ICollection<NetworkConnection> Connections { get; } = [];

    public ICollection<DnsQuery> DnsQueries { get; } = [];

    public ICollection<TlsObservation> TlsObservations { get; } = [];

    public ICollection<SecurityAlert> Alerts { get; } = [];
}
