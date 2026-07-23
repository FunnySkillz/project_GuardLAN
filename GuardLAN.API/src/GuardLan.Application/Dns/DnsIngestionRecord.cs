namespace GuardLan.Application.Dns;

public sealed record DnsIngestionRecord(
    string ClientIp,
    string Domain,
    bool WasBlocked,
    DateTime TimestampUtc);
