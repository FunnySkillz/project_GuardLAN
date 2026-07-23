using GuardLan.Domain.Entities;
using GuardLan.Domain.Enums;

namespace GuardLan.Application.Models;

public sealed record NetworkScanDto(
    Guid Id,
    string Subnet,
    NetworkScanStatus Status,
    DateTime RequestedUtc,
    DateTime? StartedUtc,
    DateTime? FinishedUtc,
    int DevicesDiscovered,
    string? Notes)
{
    public static NetworkScanDto FromEntity(NetworkScanRun scanRun)
    {
        return new NetworkScanDto(
            scanRun.Id,
            scanRun.Subnet,
            scanRun.Status,
            scanRun.RequestedUtc,
            scanRun.StartedUtc,
            scanRun.FinishedUtc,
            scanRun.DevicesDiscovered,
            scanRun.Notes);
    }
}

public sealed record QueueNetworkScanCommand(string? Subnet);
