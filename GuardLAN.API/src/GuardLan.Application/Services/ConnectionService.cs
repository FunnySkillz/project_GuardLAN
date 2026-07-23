using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using GuardLan.Domain.Entities;
using GuardLan.Domain.Repositories;

namespace GuardLan.Application.Services;

public sealed class ConnectionService(IUnitOfWork unitOfWork, TimeProvider timeProvider) : IConnectionService
{
    private const int DefaultHours = 24;
    private const int MaxHours = 168;
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 25;
    private const int MaxPageSize = 100;
    private const int MaxSearchLength = 128;

    public async Task<ConnectionOverviewDto> GetOverviewAsync(
        ConnectionOverviewQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var sanitizedHours = query.Hours <= 0 ? DefaultHours : Math.Min(query.Hours, MaxHours);
        var sanitizedPage = query.Page <= 0 ? DefaultPage : query.Page;
        var sanitizedPageSize = query.PageSize <= 0
            ? DefaultPageSize
            : Math.Min(query.PageSize, MaxPageSize);
        var sanitizedProtocol = NormalizeProtocol(query.Protocol);
        var sanitizedSearch = NormalizeSearch(query.Search);
        var sinceUtc = timeProvider.GetUtcNow().UtcDateTime.AddHours(-sanitizedHours);
        var connections = await unitOfWork.NetworkConnections.GetSinceWithDevicesAsync(
            sinceUtc,
            cancellationToken);
        var connectionPage = await unitOfWork.NetworkConnections.GetPageSinceWithDevicesAsync(
            new NetworkConnectionQuery(
                sinceUtc,
                sanitizedPage,
                sanitizedPageSize,
                sanitizedProtocol,
                sanitizedSearch),
            cancellationToken);

        return new ConnectionOverviewDto(
            BuildSummary(connections),
            BuildTopProtocols(connections),
            BuildTopDestinations(connections),
            BuildTopDevices(connections),
            new ConnectionPageDto(
                connectionPage.Items.Select(ConnectionDto.FromEntity).ToArray(),
                connectionPage.Page,
                connectionPage.PageSize,
                connectionPage.TotalCount,
                CalculateTotalPages(connectionPage.TotalCount, connectionPage.PageSize)));
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

    private static string? NormalizeProtocol(string? protocol)
    {
        var normalized = protocol?.Trim().ToLowerInvariant();

        return normalized is "tcp" or "udp" or "other" ? normalized : null;
    }

    private static string? NormalizeSearch(string? search)
    {
        var normalized = search?.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return normalized.Length <= MaxSearchLength
            ? normalized
            : normalized[..MaxSearchLength];
    }

    private static int CalculateTotalPages(int totalCount, int pageSize)
    {
        return totalCount == 0 ? 0 : (int)Math.Ceiling((double)totalCount / pageSize);
    }
}
