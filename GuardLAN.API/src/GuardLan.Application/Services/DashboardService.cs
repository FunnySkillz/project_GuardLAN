using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using GuardLan.Domain.Entities;
using GuardLan.Domain.Enums;
using GuardLan.Domain.Repositories;

namespace GuardLan.Application.Services;

public sealed class DashboardService(
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IDeviceRiskEvaluator deviceRiskEvaluator) : IDashboardService
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
        var risks = deviceRiskEvaluator.Evaluate(
            data.Devices,
            data.Alerts,
            data.RiskDnsQueries,
            data.Connections,
            data.NowUtc);

        return new DashboardOverviewDto(
            BuildSnapshot(data),
            data.Devices.Select(device => DeviceDto.FromEntity(device, risks[device.Id])).ToArray(),
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
        var riskDnsQueries = todayUtc == sinceUtc
            ? dnsQueries
            : await unitOfWork.DnsQueries.GetSinceAsync(sinceUtc, cancellationToken);
        var connections = await unitOfWork.NetworkConnections.GetSinceAsync(sinceUtc, cancellationToken);

        return new DashboardData(devices, alerts, dnsQueries, riskDnsQueries, connections, nowUtc, todayUtc);
    }

    private static DashboardSnapshotDto BuildSnapshot(DashboardData data)
    {
        var deviceLookup = data.Devices.ToDictionary(device => device.Id);

        var connectionTraffic = new TrafficSummaryDto(
            data.Connections.Count,
            data.Connections.Select(connection => connection.DeviceId).Distinct().Count(),
            data.Connections
                .Select(GetDestinationKey)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count(),
            data.Connections.Sum(connection => connection.BytesSent),
            data.Connections.Sum(connection => connection.BytesReceived));

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

        var topProtocols = data.Connections
            .GroupBy(connection => NormalizeProtocolLabel(connection.Protocol), StringComparer.OrdinalIgnoreCase)
            .Select(group => new ProtocolActivityDto(
                group.Key,
                group.Count(),
                group.Sum(connection => connection.BytesSent),
                group.Sum(connection => connection.BytesReceived)))
            .OrderByDescending(protocol => protocol.BytesSent + protocol.BytesReceived)
            .ThenBy(protocol => protocol.Protocol)
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
            connectionTraffic,
            mostActiveDevices,
            topProtocols,
            topDomains,
            recentAlerts);
    }

    private static string GetDestinationKey(NetworkConnection connection)
    {
        return string.IsNullOrWhiteSpace(connection.DestinationDomain)
            ? connection.DestinationIp
            : connection.DestinationDomain;
    }

    private static string NormalizeProtocolLabel(string protocol)
    {
        return string.IsNullOrWhiteSpace(protocol) ? "OTHER" : protocol.Trim().ToUpperInvariant();
    }

    private sealed record DashboardData(
        IReadOnlyList<NetworkDevice> Devices,
        IReadOnlyList<SecurityAlert> Alerts,
        IReadOnlyList<DnsQuery> DnsQueries,
        IReadOnlyList<DnsQuery> RiskDnsQueries,
        IReadOnlyList<NetworkConnection> Connections,
        DateTime NowUtc,
        DateTime TodayUtc);
}
