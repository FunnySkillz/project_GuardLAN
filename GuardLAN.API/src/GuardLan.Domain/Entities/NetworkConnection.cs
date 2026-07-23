namespace GuardLan.Domain.Entities;

public class NetworkConnection
{
    public Guid Id { get; set; }

    public Guid DeviceId { get; set; }

    public NetworkDevice? Device { get; set; }

    public string? Source { get; set; }

    public string? SourceRecordId { get; set; }

    public string DestinationIp { get; set; } = null!;

    public string? DestinationDomain { get; set; }

    public string Protocol { get; set; } = null!;

    public int? DestinationPort { get; set; }

    public long BytesSent { get; set; }

    public long BytesReceived { get; set; }

    public DateTime FirstSeenUtc { get; set; }

    public DateTime LastSeenUtc { get; set; }

    public ICollection<TlsObservation> TlsObservations { get; } = [];
}
