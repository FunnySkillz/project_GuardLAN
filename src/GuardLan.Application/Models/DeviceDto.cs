using GuardLan.Domain.Entities;
using GuardLan.Domain.Enums;

namespace GuardLan.Application.Models;

public sealed record DeviceDto(
    Guid Id,
    string IpAddress,
    string MacAddress,
    string? Hostname,
    string? Vendor,
    DeviceType DeviceType,
    bool IsTrusted,
    DateTime FirstSeenUtc,
    DateTime LastSeenUtc,
    bool IsOnline)
{
    public static DeviceDto FromEntity(NetworkDevice device)
    {
        return new DeviceDto(
            device.Id,
            device.IpAddress,
            device.MacAddress,
            device.Hostname,
            device.Vendor,
            device.DeviceType,
            device.IsTrusted,
            device.FirstSeenUtc,
            device.LastSeenUtc,
            device.IsOnline);
    }
}

public sealed record UpdateDeviceCommand(
    string? Hostname,
    DeviceType? DeviceType,
    bool? IsTrusted);
