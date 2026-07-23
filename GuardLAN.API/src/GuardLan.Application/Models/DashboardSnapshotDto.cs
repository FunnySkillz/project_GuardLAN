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
    TrafficSummaryDto ConnectionTraffic,
    IReadOnlyList<DeviceActivityDto> MostActiveDevices,
    IReadOnlyList<ProtocolActivityDto> TopConnectionProtocols,
    IReadOnlyList<DomainActivityDto> MostContactedExternalDomains,
    IReadOnlyList<AlertDto> RecentAlerts);

public sealed record DashboardOverviewDto(
    DashboardSnapshotDto Summary,
    IReadOnlyList<DeviceDto> Devices,
    IReadOnlyList<NetworkScanDto> RecentScans);

public sealed record DeviceActivityDto(
    Guid DeviceId,
    string Name,
    string IpAddress,
    long BytesSent,
    long BytesReceived,
    int ConnectionCount);

public sealed record TrafficSummaryDto(
    int TotalConnections,
    int ActiveDevices,
    int UniqueDestinations,
    long BytesSent,
    long BytesReceived);

public sealed record ProtocolActivityDto(
    string Protocol,
    int Connections,
    long BytesSent,
    long BytesReceived);

public sealed record DomainActivityDto(
    string Domain,
    int Requests,
    int BlockedRequests);
