using GuardLan.Application.Models;
using GuardLan.Application.Services;
using GuardLan.Domain.Entities;
using GuardLan.Domain.Enums;

namespace GuardLan.Tests;

public sealed class DeviceRiskEvaluatorTests
{
    [Fact]
    public void EvaluateReturnsNormalRiskWhenNoRecentEvidenceExists()
    {
        var nowUtc = new DateTime(2026, 7, 23, 12, 0, 0, DateTimeKind.Utc);
        var device = CreateDevice(nowUtc, trusted: true, deviceType: DeviceType.Desktop);
        var evaluator = new DeviceRiskEvaluator();

        var result = evaluator.Evaluate([device], [], [], [], nowUtc);

        Assert.Equal(DeviceRiskLevel.Normal, result[device.Id].Level);
        Assert.Equal(0, result[device.Id].Score);
        Assert.Contains("No recent risk evidence.", result[device.Id].Reasons);
    }

    [Fact]
    public void EvaluateCombinesOpenAlertsAndTelemetryIntoExplainableRisk()
    {
        var nowUtc = new DateTime(2026, 7, 23, 12, 0, 0, DateTimeKind.Utc);
        var device = CreateDevice(nowUtc, trusted: false, deviceType: DeviceType.Unknown, recentlySeen: true);
        var evaluator = new DeviceRiskEvaluator();

        var result = evaluator.Evaluate(
            [device],
            [
                new SecurityAlert
                {
                    DeviceId = device.Id,
                    Severity = AlertSeverity.High,
                    Type = "Suricata",
                    Message = "Suspicious TLS flow",
                    CreatedUtc = nowUtc.AddMinutes(-10)
                }
            ],
            [
                new DnsQuery
                {
                    DeviceId = device.Id,
                    ClientIp = device.IpAddress,
                    Domain = "blocked.example",
                    WasBlocked = true,
                    TimestampUtc = nowUtc.AddMinutes(-5)
                }
            ],
            [
                new NetworkConnection
                {
                    DeviceId = device.Id,
                    DestinationIp = "203.0.113.10",
                    Protocol = "tcp",
                    BytesSent = 1024,
                    BytesReceived = 1024,
                    FirstSeenUtc = nowUtc.AddMinutes(-5),
                    LastSeenUtc = nowUtc
                }
            ],
            nowUtc);

        var risk = result[device.Id];

        Assert.Equal(DeviceRiskLevel.Critical, risk.Level);
        Assert.Equal(100, risk.Score);
        Assert.Contains("1 open high-severity alert.", risk.Reasons);
        Assert.Contains("Device is not marked trusted.", risk.Reasons);
        Assert.Contains("Device type is still unknown.", risk.Reasons);
        Assert.Contains("Device was first seen in the last 24 hours.", risk.Reasons);
    }

    [Fact]
    public void EvaluateIgnoresResolvedAlerts()
    {
        var nowUtc = new DateTime(2026, 7, 23, 12, 0, 0, DateTimeKind.Utc);
        var device = CreateDevice(nowUtc, trusted: true, deviceType: DeviceType.Desktop);
        var evaluator = new DeviceRiskEvaluator();

        var result = evaluator.Evaluate(
            [device],
            [
                new SecurityAlert
                {
                    DeviceId = device.Id,
                    Severity = AlertSeverity.Critical,
                    Type = "Suricata",
                    Message = "Resolved alert",
                    CreatedUtc = nowUtc.AddHours(-2),
                    ResolvedUtc = nowUtc.AddHours(-1)
                }
            ],
            [],
            [],
            nowUtc);

        Assert.Equal(DeviceRiskLevel.Normal, result[device.Id].Level);
    }

    private static NetworkDevice CreateDevice(
        DateTime nowUtc,
        bool trusted,
        DeviceType deviceType,
        bool recentlySeen = false)
    {
        return new NetworkDevice
        {
            Id = Guid.NewGuid(),
            IpAddress = "192.168.1.22",
            MacAddress = "02:00:00:00:00:22",
            DeviceType = deviceType,
            FirstSeenUtc = recentlySeen ? nowUtc.AddHours(-1) : nowUtc.AddDays(-2),
            LastSeenUtc = nowUtc,
            IsOnline = true,
            IsTrusted = trusted
        };
    }
}
