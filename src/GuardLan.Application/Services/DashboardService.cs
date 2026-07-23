using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using GuardLan.Domain.Enums;

namespace GuardLan.Application.Services;

public sealed class DashboardService(IGuardLanRepository repository, TimeProvider timeProvider) : IDashboardService
{
    public async Task<DashboardSnapshotDto> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var todayUtc = nowUtc.Date;
        var sinceUtc = nowUtc.AddHours(-24);

        var devices = await repository.ListDevicesAsync(cancellationToken);
        var alerts = await repository.ListAlertsAsync(cancellationToken);
        var dnsQueries = await repository.ListDnsQueriesSinceAsync(todayUtc, cancellationToken);
        var connections = await repository.ListConnectionsSinceAsync(sinceUtc, cancellationToken);

        var deviceLookup = devices.ToDictionary(device => device.Id);

        var mostActiveDevices = connections
            .GroupBy(connection => connection.DeviceId)
            .Select(group =>
            {
                deviceLookup.TryGetValue(group.Key, out var device);

                return new DeviceActivityDto(
                    group.Key,
                    device?.Hostname ?? device?.IpAddress ?? "Unknown device",
                    device?.IpAddress ?? "0.0.0.0",
                    group.Sum(connection => connection.BytesSent),
                    group.Sum(connection => connection.BytesReceived),
                    group.Count());
            })
            .OrderByDescending(activity => activity.BytesSent + activity.BytesReceived)
            .Take(5)
            .ToArray();

        var topDomains = dnsQueries
            .GroupBy(query => query.Domain)
            .Select(group => new DomainActivityDto(
                group.Key,
                group.Count(),
                group.Count(query => query.WasBlocked)))
            .OrderByDescending(domain => domain.Requests)
            .Take(5)
            .ToArray();

        var recentAlerts = alerts
            .Where(alert => alert.ResolvedUtc is null)
            .OrderByDescending(alert => alert.CreatedUtc)
            .Take(5)
            .Select(AlertDto.FromEntity)
            .ToArray();

        return new DashboardSnapshotDto(
            devices.Count(device => device.IsOnline),
            devices.Count(device => device.DeviceType == DeviceType.Unknown || !device.IsTrusted),
            devices.Count(device => device.FirstSeenUtc >= todayUtc),
            devices.Count(device => device.IsTrusted),
            dnsQueries.Count,
            dnsQueries.Count(query => query.WasBlocked),
            alerts.Count(alert => alert.ResolvedUtc is null),
            alerts.Count(alert => alert.ResolvedUtc is null && alert.Severity >= AlertSeverity.Critical),
            mostActiveDevices,
            topDomains,
            recentAlerts);
    }
}
