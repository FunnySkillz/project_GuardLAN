namespace GuardLan.Domain.Entities;

public class TlsObservation
{
    public Guid Id { get; set; }

    public Guid? DeviceId { get; set; }

    public NetworkDevice? Device { get; set; }

    public Guid? ConnectionId { get; set; }

    public NetworkConnection? Connection { get; set; }

    public string Source { get; set; } = null!;

    public string? SourceRecordId { get; set; }

    public string SourceIp { get; set; } = null!;

    public string DestinationIp { get; set; } = null!;

    public int? DestinationPort { get; set; }

    public string? ServerName { get; set; }

    public string? Version { get; set; }

    public string? Cipher { get; set; }

    public string? Ja3 { get; set; }

    public string? Ja3s { get; set; }

    public string? Alpn { get; set; }

    public bool? WasEstablished { get; set; }

    public DateTime ObservedUtc { get; set; }
}
