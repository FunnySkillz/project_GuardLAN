using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using GuardLan.Domain.Entities;
using GuardLan.Domain.Enums;
using GuardLan.Domain.Repositories;

namespace GuardLan.Application.Services;

public sealed class DashboardService(IUnitOfWork unitOfWork, TimeProvider timeProvider) : IDashboardService
{
    public async Task<DashboardSnapshotDto> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        var data = await LoadDashboardDataAsync(cancellationToken);

        return BuildSnapshot(data);
    }

    public async Task<DashboardOverviewDto> GetOverviewAsync(CancellationToken cancellationToken)
    {
        var data = await LoadDashboardDataAsync(cancellationToken);
        var scanRuns = await unitOfWork.NetworkScanRuns.GetRecentAsync(cancellationToken);

        return new DashboardOverviewDto(
            BuildSnapshot(data),
            data.Devices.Select(DeviceDto.FromEntity).ToArray(),
            scanRuns.Select(NetworkScanDto.FromEntity).ToArray());
    }

    private async Task<DashboardData> LoadDashboardDataAsync(CancellationToken cancellationToken)
    {
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var todayUtc = nowUtc.Date;
        var sinceUtc = nowUtc.AddHours(-24);

        var devices = await unitOfWork.Devices.GetInventoryAsync(cancellationToken);
        var alerts = await unitOfWork.SecurityAlerts.GetRecentAsync(cancellationToken);
        var dnsQueries = await unitOfWork.DnsQueries.GetSinceAsync(todayUtc, cancellationToken);
        var connections = await unitOfWork.NetworkConnections.GetSinceAsync(sinceUtc, cancellationToken);

        return new DashboardData(devices, alerts, dnsQueries, connections, todayUtc);
    }

    private static DashboardSnapshotDto BuildSnapshot(DashboardData data)
    {
        var deviceLookup = data.Devices.ToDictionary(device => device.Id);

        var mostActiveDevices = data.Connections
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

        var topDomains = data.DnsQueries
            .GroupBy(query => query.Domain)
            .Select(group => new DomainActivityDto(
                group.Key,
                group.Count(),
                group.Count(query => query.WasBlocked)))
            .OrderByDescending(domain => domain.Requests)
            .Take(5)
            .ToArray();

        var recentAlerts = data.Alerts
            .Where(alert => alert.ResolvedUtc is null)
            .OrderByDescending(alert => alert.CreatedUtc)
            .Take(5)
            .Select(AlertDto.FromEntity)
            .ToArray();

        return new DashboardSnapshotDto(
            data.Devices.Count(device => device.IsOnline),
            data.Devices.Count(device => device.DeviceType == DeviceType.Unknown || !device.IsTrusted),
            data.Devices.Count(device => device.FirstSeenUtc >= data.TodayUtc),
            data.Devices.Count(device => device.IsTrusted),
            data.DnsQueries.Count,
            data.DnsQueries.Count(query => query.WasBlocked),
            data.Alerts.Count(alert => alert.ResolvedUtc is null),
            data.Alerts.Count(alert => alert.ResolvedUtc is null && alert.Severity >= AlertSeverity.Critical),
            mostActiveDevices,
            topDomains,
            recentAlerts);
    }

    private sealed record DashboardData(
        IReadOnlyList<NetworkDevice> Devices,
        IReadOnlyList<SecurityAlert> Alerts,
        IReadOnlyList<DnsQuery> DnsQueries,
        IReadOnlyList<NetworkConnection> Connections,
        DateTime TodayUtc);
}
