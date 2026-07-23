using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using GuardLan.Domain.Entities;
using GuardLan.Domain.Repositories;

namespace GuardLan.Application.Services;

public sealed class ConnectionService(IUnitOfWork unitOfWork, TimeProvider timeProvider) : IConnectionService
{
    private const int DefaultHours = 24;
    private const int MaxHours = 168;
    private const int DefaultLimit = 100;
    private const int MaxLimit = 500;

    public async Task<ConnectionOverviewDto> GetOverviewAsync(
        int hours,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var sanitizedHours = hours <= 0 ? DefaultHours : Math.Min(hours, MaxHours);
        var sanitizedLimit = limit <= 0 ? DefaultLimit : Math.Min(limit, MaxLimit);
        var sinceUtc = timeProvider.GetUtcNow().UtcDateTime.AddHours(-sanitizedHours);
        var connections = await unitOfWork.NetworkConnections.GetSinceWithDevicesAsync(
            sinceUtc,
            cancellationToken);
        var orderedConnections = connections
            .OrderByDescending(connection => connection.LastSeenUtc)
            .ToArray();

        return new ConnectionOverviewDto(
            BuildSummary(connections),
            BuildTopProtocols(connections),
            BuildTopDestinations(connections),
            BuildTopDevices(connections),
            orderedConnections
                .Take(sanitizedLimit)
                .Select(ConnectionDto.FromEntity)
                .ToArray());
    }

    private static ConnectionOverviewSummaryDto BuildSummary(IReadOnlyList<NetworkConnection> connections)
    {
        return new ConnectionOverviewSummaryDto(
            connections.Count,
            connections.Select(connection => connection.DeviceId).Distinct().Count(),
            connections
                .Select(GetDestinationKey)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count(),
            connections.Sum(connection => connection.BytesSent),
            connections.Sum(connection => connection.BytesReceived));
    }

    private static IReadOnlyList<ConnectionProtocolSummaryDto> BuildTopProtocols(
        IReadOnlyList<NetworkConnection> connections)
    {
        return connections
            .GroupBy(connection => connection.Protocol, StringComparer.OrdinalIgnoreCase)
            .Select(group => new ConnectionProtocolSummaryDto(
                group.Key,
                group.Count(),
                group.Sum(connection => connection.BytesSent),
                group.Sum(connection => connection.BytesReceived)))
            .OrderByDescending(protocol => protocol.BytesSent + protocol.BytesReceived)
            .ThenBy(protocol => protocol.Protocol)
            .Take(10)
            .ToArray();
    }

    private static IReadOnlyList<ConnectionDestinationSummaryDto> BuildTopDestinations(
        IReadOnlyList<NetworkConnection> connections)
    {
        return connections
            .GroupBy(GetDestinationKey, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var first = group.First();

                return new ConnectionDestinationSummaryDto(
                    group.Key,
                    first.DestinationIp,
                    group.Count(),
                    group.Sum(connection => connection.BytesSent),
                    group.Sum(connection => connection.BytesReceived));
            })
            .OrderByDescending(destination => destination.BytesSent + destination.BytesReceived)
            .ThenBy(destination => destination.Destination)
            .Take(10)
            .ToArray();
    }

    private static IReadOnlyList<ConnectionDeviceSummaryDto> BuildTopDevices(
        IReadOnlyList<NetworkConnection> connections)
    {
        return connections
            .GroupBy(connection => new
            {
                connection.DeviceId,
                DeviceName = connection.Device?.Hostname ?? connection.Device?.IpAddress ?? "Unknown device",
                DeviceIp = connection.Device?.IpAddress ?? "0.0.0.0"
            })
            .Select(group => new ConnectionDeviceSummaryDto(
                group.Key.DeviceId,
                group.Key.DeviceName,
                group.Key.DeviceIp,
                group.Count(),
                group.Sum(connection => connection.BytesSent),
                group.Sum(connection => connection.BytesReceived)))
            .OrderByDescending(device => device.BytesSent + device.BytesReceived)
            .ThenBy(device => device.DeviceName)
            .Take(10)
            .ToArray();
    }

    private static string GetDestinationKey(NetworkConnection connection)
    {
        return string.IsNullOrWhiteSpace(connection.DestinationDomain)
            ? connection.DestinationIp
            : connection.DestinationDomain;
    }
}
