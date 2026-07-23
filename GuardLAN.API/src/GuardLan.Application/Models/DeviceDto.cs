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
    bool IsOnline,
    DeviceRiskDto Risk)
{
    public static DeviceDto FromEntity(NetworkDevice device, DeviceRiskDto? risk = null)
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
            device.IsOnline,
            risk ?? DeviceRiskDto.Normal);
    }
}

public sealed record UpdateDeviceCommand(
    string? Hostname,
    DeviceType? DeviceType,
    bool? IsTrusted);
