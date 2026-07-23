using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using GuardLan.Domain.Entities;
using GuardLan.Domain.Repositories;

namespace GuardLan.Application.Services;

public sealed class DnsService(IUnitOfWork unitOfWork, TimeProvider timeProvider) : IDnsService
{
    private const int DefaultHours = 24;
    private const int MaxHours = 168;
    private const int DefaultLimit = 100;
    private const int MaxLimit = 500;

    public async Task<DnsOverviewDto> GetOverviewAsync(
        int hours,
        int limit,
        CancellationToken cancellationToken)
    {
        var sanitizedHours = hours <= 0 ? DefaultHours : Math.Min(hours, MaxHours);
        var sanitizedLimit = limit <= 0 ? DefaultLimit : Math.Min(limit, MaxLimit);
        var sinceUtc = timeProvider.GetUtcNow().UtcDateTime.AddHours(-sanitizedHours);
        var queries = await unitOfWork.DnsQueries.GetSinceWithDevicesAsync(sinceUtc, cancellationToken);
        var orderedQueries = queries
            .OrderByDescending(query => query.TimestampUtc)
            .ToArray();

        return new DnsOverviewDto(
            BuildSummary(queries),
            BuildTopDomains(queries),
            BuildTopClients(queries),
            orderedQueries
                .Take(sanitizedLimit)
                .Select(DnsQueryDto.FromEntity)
                .ToArray());
    }

    private static DnsOverviewSummaryDto BuildSummary(IReadOnlyList<DnsQuery> queries)
    {
        var blockedQueries = queries.Count(query => query.WasBlocked);

        return new DnsOverviewSummaryDto(
            queries.Count,
            queries.Count - blockedQueries,
            blockedQueries,
            queries.Select(query => query.Domain).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            queries.Select(query => query.ClientIp).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    private static IReadOnlyList<DnsDomainSummaryDto> BuildTopDomains(IReadOnlyList<DnsQuery> queries)
    {
        return queries
            .GroupBy(query => query.Domain, StringComparer.OrdinalIgnoreCase)
            .Select(group => new DnsDomainSummaryDto(
                group.Key,
                group.Count(),
                group.Count(query => query.WasBlocked)))
            .OrderByDescending(domain => domain.Requests)
            .ThenBy(domain => domain.Domain)
            .Take(10)
            .ToArray();
    }

    private static IReadOnlyList<DnsClientSummaryDto> BuildTopClients(IReadOnlyList<DnsQuery> queries)
    {
        return queries
            .GroupBy(query => new
            {
                query.DeviceId,
                DeviceName = query.Device?.Hostname ?? query.Device?.IpAddress,
                query.ClientIp
            })
            .Select(group => new DnsClientSummaryDto(
                group.Key.DeviceId,
                group.Key.DeviceName,
                group.Key.ClientIp,
                group.Count(),
                group.Count(query => query.WasBlocked)))
            .OrderByDescending(client => client.Requests)
            .ThenBy(client => client.ClientIp)
            .Take(10)
            .ToArray();
    }
}
