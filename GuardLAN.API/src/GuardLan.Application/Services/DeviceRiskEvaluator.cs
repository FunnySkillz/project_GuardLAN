using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using GuardLan.Domain.Entities;
using GuardLan.Domain.Enums;

namespace GuardLan.Application.Services;

public sealed class DeviceRiskEvaluator : IDeviceRiskEvaluator
{
    private const long HighTrafficBytes = 1024L * 1024L * 1024L;

    public IReadOnlyDictionary<Guid, DeviceRiskDto> Evaluate(
        IReadOnlyList<NetworkDevice> devices,
        IReadOnlyList<SecurityAlert> alerts,
        IReadOnlyList<DnsQuery> dnsQueries,
        IReadOnlyList<NetworkConnection> connections,
        DateTime nowUtc)
    {
        var openAlertsByDevice = alerts
            .Where(alert => alert.DeviceId.HasValue && alert.ResolvedUtc is null)
            .GroupBy(alert => alert.DeviceId!.Value)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var recentBlockedDnsByDevice = dnsQueries
            .Where(query => query.DeviceId.HasValue && query.WasBlocked)
            .GroupBy(query => query.DeviceId!.Value)
            .ToDictionary(group => group.Key, group => group.Count());
        var recentTrafficByDevice = connections
            .GroupBy(connection => connection.DeviceId)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(connection => connection.BytesSent + connection.BytesReceived));

        return devices.ToDictionary(
            device => device.Id,
            device => EvaluateDevice(
                device,
                openAlertsByDevice.GetValueOrDefault(device.Id) ?? [],
                recentBlockedDnsByDevice.GetValueOrDefault(device.Id),
                recentTrafficByDevice.GetValueOrDefault(device.Id),
                nowUtc));
    }

    private static DeviceRiskDto EvaluateDevice(
        NetworkDevice device,
        IReadOnlyList<SecurityAlert> openAlerts,
        int blockedDnsRequests,
        long recentTrafficBytes,
        DateTime nowUtc)
    {
        var score = 0;
        var reasons = new List<string>();

        var criticalAlerts = openAlerts.Count(alert => alert.Severity == AlertSeverity.Critical);
        var highAlerts = openAlerts.Count(alert => alert.Severity == AlertSeverity.High);
        var mediumAlerts = openAlerts.Count(alert => alert.Severity == AlertSeverity.Medium);

        if (criticalAlerts > 0)
        {
            score += 80;
            reasons.Add($"{criticalAlerts} open critical alert{Plural(criticalAlerts)}.");
        }
        else if (highAlerts > 0)
        {
            score += 60;
            reasons.Add($"{highAlerts} open high-severity alert{Plural(highAlerts)}.");
        }
        else if (mediumAlerts > 0)
        {
            score += 35;
            reasons.Add($"{mediumAlerts} open medium-severity alert{Plural(mediumAlerts)}.");
        }

        if (!device.IsTrusted)
        {
            score += 25;
            reasons.Add("Device is not marked trusted.");
        }

        if (device.DeviceType == DeviceType.Unknown)
        {
            score += 15;
            reasons.Add("Device type is still unknown.");
        }

        if (device.FirstSeenUtc >= nowUtc.AddHours(-24))
        {
            score += device.IsTrusted ? 5 : 20;
            reasons.Add("Device was first seen in the last 24 hours.");
        }

        if (blockedDnsRequests > 0)
        {
            score += blockedDnsRequests >= 10 ? 30 : 15;
            reasons.Add($"{blockedDnsRequests} blocked DNS request{Plural(blockedDnsRequests)} in the current window.");
        }

        if (recentTrafficBytes >= HighTrafficBytes)
        {
            score += 10;
            reasons.Add("Recent traffic volume is above 1 GB.");
        }

        var cappedScore = Math.Clamp(score, 0, 100);

        return reasons.Count == 0
            ? DeviceRiskDto.Normal
            : new DeviceRiskDto(MapRiskLevel(cappedScore), cappedScore, reasons.Take(4).ToArray());
    }

    private static DeviceRiskLevel MapRiskLevel(int score)
    {
        return score switch
        {
            >= 80 => DeviceRiskLevel.Critical,
            >= 55 => DeviceRiskLevel.High,
            >= 30 => DeviceRiskLevel.Medium,
            > 0 => DeviceRiskLevel.Low,
            _ => DeviceRiskLevel.Normal
        };
    }

    private static string Plural(int count)
    {
        return count == 1 ? string.Empty : "s";
    }
}
