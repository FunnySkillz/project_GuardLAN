namespace GuardLan.Application.Models;

public sealed record DashboardSnapshotDto(
    int OnlineDevices,
    int UnknownDevices,
    int NewDevicesToday,
    int TrustedDevices,
    int DnsRequestsToday,
    int BlockedDomainsToday,
    int OpenAlerts,
    int CriticalAlerts,
    IReadOnlyList<DeviceActivityDto> MostActiveDevices,
    IReadOnlyList<DomainActivityDto> MostContactedExternalDomains,
    IReadOnlyList<AlertDto> RecentAlerts);

public sealed record DeviceActivityDto(
    Guid DeviceId,
    string Name,
    string IpAddress,
    long BytesSent,
    long BytesReceived,
    int ConnectionCount);

public sealed record DomainActivityDto(
    string Domain,
    int Requests,
    int BlockedRequests);
