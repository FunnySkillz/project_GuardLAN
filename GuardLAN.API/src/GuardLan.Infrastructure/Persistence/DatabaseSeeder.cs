using GuardLan.Domain.Entities;
using GuardLan.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GuardLan.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task InitializeAsync(
        GuardLanDbContext dbContext,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        if (await dbContext.Devices.AnyAsync(cancellationToken))
        {
            return;
        }

        var nowUtc = DateTime.UtcNow;
        var routerId = Guid.Parse("8cc5e1fe-7ba4-4e3f-b04d-5acbd51fcb5a");
        var desktopId = Guid.Parse("20f26ca4-284b-41df-9554-8a650f6c0dc7");
        var tvId = Guid.Parse("349a8568-a579-4971-88a8-cc331c78652f");
        var unknownId = Guid.Parse("84c5f8b8-7f68-4c4b-a8e5-340b35a59ba7");

        var devices = new[]
        {
            new NetworkDevice
            {
                Id = routerId,
                IpAddress = "192.168.1.1",
                MacAddress = "02:00:00:00:00:01",
                Hostname = "gateway",
                Vendor = "OPNsense",
                DeviceType = DeviceType.Router,
                IsTrusted = true,
                FirstSeenUtc = nowUtc.AddDays(-120),
                LastSeenUtc = nowUtc.AddMinutes(-1),
                IsOnline = true
            },
            new NetworkDevice
            {
                Id = desktopId,
                IpAddress = "192.168.1.22",
                MacAddress = "02:00:00:00:00:22",
                Hostname = "desktop-pc",
                Vendor = "Intel",
                DeviceType = DeviceType.Desktop,
                IsTrusted = true,
                FirstSeenUtc = nowUtc.AddDays(-48),
                LastSeenUtc = nowUtc.AddMinutes(-3),
                IsOnline = true
            },
            new NetworkDevice
            {
                Id = tvId,
                IpAddress = "192.168.1.32",
                MacAddress = "AA:BB:CC:DD:EE:FF",
                Hostname = "living-room-tv",
                Vendor = "Samsung",
                DeviceType = DeviceType.SmartTv,
                IsTrusted = true,
                FirstSeenUtc = nowUtc.AddDays(-21),
                LastSeenUtc = nowUtc.AddMinutes(-2),
                IsOnline = true
            },
            new NetworkDevice
            {
                Id = unknownId,
                IpAddress = "192.168.1.71",
                MacAddress = "02:00:00:00:00:71",
                Hostname = null,
                Vendor = null,
                DeviceType = DeviceType.Unknown,
                IsTrusted = false,
                FirstSeenUtc = nowUtc.AddHours(-2),
                LastSeenUtc = nowUtc.AddMinutes(-4),
                IsOnline = true
            }
        };

        var dnsQueries = new[]
        {
            new DnsQuery
            {
                Id = Guid.NewGuid(),
                DeviceId = desktopId,
                ClientIp = "192.168.1.22",
                Domain = "github.com",
                WasBlocked = false,
                TimestampUtc = nowUtc.AddMinutes(-16)
            },
            new DnsQuery
            {
                Id = Guid.NewGuid(),
                DeviceId = tvId,
                ClientIp = "192.168.1.32",
                Domain = "ads.streaming.example",
                WasBlocked = true,
                TimestampUtc = nowUtc.AddMinutes(-11)
            },
            new DnsQuery
            {
                Id = Guid.NewGuid(),
                DeviceId = unknownId,
                ClientIp = "192.168.1.71",
                Domain = "new-device-check.example",
                WasBlocked = false,
                TimestampUtc = nowUtc.AddMinutes(-8)
            }
        };

        var connections = new[]
        {
            new NetworkConnection
            {
                Id = Guid.NewGuid(),
                DeviceId = desktopId,
                DestinationIp = "140.82.112.4",
                DestinationDomain = "github.com",
                Protocol = "TCP",
                DestinationPort = 443,
                BytesSent = 488_332,
                BytesReceived = 2_138_740,
                FirstSeenUtc = nowUtc.AddMinutes(-22),
                LastSeenUtc = nowUtc.AddMinutes(-4)
            },
            new NetworkConnection
            {
                Id = Guid.NewGuid(),
                DeviceId = tvId,
                DestinationIp = "198.51.100.42",
                DestinationDomain = "streaming.example",
                Protocol = "TCP",
                DestinationPort = 443,
                BytesSent = 1_144_128,
                BytesReceived = 84_229_376,
                FirstSeenUtc = nowUtc.AddMinutes(-41),
                LastSeenUtc = nowUtc.AddMinutes(-2)
            },
            new NetworkConnection
            {
                Id = Guid.NewGuid(),
                DeviceId = unknownId,
                DestinationIp = "203.0.113.80",
                DestinationDomain = "new-device-check.example",
                Protocol = "UDP",
                DestinationPort = 443,
                BytesSent = 93_382,
                BytesReceived = 104_210,
                FirstSeenUtc = nowUtc.AddMinutes(-8),
                LastSeenUtc = nowUtc.AddMinutes(-7)
            }
        };

        var alerts = new[]
        {
            new SecurityAlert
            {
                Id = Guid.NewGuid(),
                DeviceId = unknownId,
                Severity = AlertSeverity.High,
                Type = "UnknownDeviceConnected",
                Message = "New unknown device connected at 192.168.1.71.",
                CreatedUtc = nowUtc.AddHours(-2)
            },
            new SecurityAlert
            {
                Id = Guid.NewGuid(),
                DeviceId = unknownId,
                Severity = AlertSeverity.Medium,
                Type = "NewDomainObserved",
                Message = "Unknown device contacted a domain that has not been observed before.",
                CreatedUtc = nowUtc.AddMinutes(-8)
            }
        };

        dbContext.Devices.AddRange(devices);
        dbContext.DnsQueries.AddRange(dnsQueries);
        dbContext.Connections.AddRange(connections);
        dbContext.Alerts.AddRange(alerts);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded GuardLAN development data.");
    }
}
