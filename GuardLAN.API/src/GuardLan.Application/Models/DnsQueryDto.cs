using GuardLan.Domain.Entities;

namespace GuardLan.Application.Models;

public sealed record DnsOverviewDto(
    DnsOverviewSummaryDto Summary,
    IReadOnlyList<DnsDomainSummaryDto> TopDomains,
    IReadOnlyList<DnsClientSummaryDto> TopClients,
    IReadOnlyList<DnsQueryDto> RecentQueries);

public sealed record DnsOverviewSummaryDto(
    int TotalQueries,
    int AllowedQueries,
    int BlockedQueries,
    int UniqueDomains,
    int ActiveClients);

public sealed record DnsDomainSummaryDto(
    string Domain,
    int Requests,
    int BlockedRequests);

public sealed record DnsClientSummaryDto(
    Guid? DeviceId,
    string? DeviceName,
    string ClientIp,
    int Requests,
    int BlockedRequests);

public sealed record DnsQueryDto(
    Guid Id,
    Guid? DeviceId,
    string? DeviceName,
    string ClientIp,
    string Domain,
    bool WasBlocked,
    DateTime TimestampUtc)
{
    public static DnsQueryDto FromEntity(DnsQuery query)
    {
        return new DnsQueryDto(
            query.Id,
            query.DeviceId,
            query.Device?.Hostname ?? query.Device?.IpAddress,
            query.ClientIp,
            query.Domain,
            query.WasBlocked,
            query.TimestampUtc);
    }
}
