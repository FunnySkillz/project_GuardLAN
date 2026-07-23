namespace GuardLan.Application.Models;

public sealed record DeviceEvidenceDto(
    DeviceDto Device,
    DeviceEvidenceSummaryDto Summary,
    IReadOnlyList<AlertDto> RecentAlerts,
    IReadOnlyList<DnsQueryDto> RecentDnsQueries,
    IReadOnlyList<ConnectionDto> RecentConnections);

public sealed record DeviceEvidenceSummaryDto(
    DateTime SinceUtc,
    int Alerts,
    int OpenAlerts,
    int DnsQueries,
    int BlockedDnsQueries,
    int UniqueDomains,
    int Connections,
    int UniqueDestinations,
    long BytesSent,
    long BytesReceived);
