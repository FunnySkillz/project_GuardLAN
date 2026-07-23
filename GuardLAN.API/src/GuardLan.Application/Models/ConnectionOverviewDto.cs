using GuardLan.Domain.Entities;

namespace GuardLan.Application.Models;

public sealed record ConnectionOverviewDto(
    ConnectionOverviewSummaryDto Summary,
    IReadOnlyList<ConnectionProtocolSummaryDto> TopProtocols,
    IReadOnlyList<ConnectionDestinationSummaryDto> TopDestinations,
    IReadOnlyList<ConnectionDeviceSummaryDto> TopDevices,
    IReadOnlyList<ConnectionDto> RecentConnections);

public sealed record ConnectionOverviewSummaryDto(
    int TotalConnections,
    int ActiveDevices,
    int UniqueDestinations,
    long TotalBytesSent,
    long TotalBytesReceived);

public sealed record ConnectionProtocolSummaryDto(
    string Protocol,
    int Connections,
    long BytesSent,
    long BytesReceived);

public sealed record ConnectionDestinationSummaryDto(
    string Destination,
    string DestinationIp,
    int Connections,
    long BytesSent,
    long BytesReceived);

public sealed record ConnectionDeviceSummaryDto(
    Guid DeviceId,
    string DeviceName,
    string DeviceIp,
    int Connections,
    long BytesSent,
    long BytesReceived);

public sealed record ConnectionDto(
    Guid Id,
    Guid DeviceId,
    string DeviceName,
    string DeviceIp,
    string DestinationIp,
    string? DestinationDomain,
    string Protocol,
    int? DestinationPort,
    long BytesSent,
    long BytesReceived,
    DateTime FirstSeenUtc,
    DateTime LastSeenUtc)
{
    public static ConnectionDto FromEntity(NetworkConnection connection)
    {
        return new ConnectionDto(
            connection.Id,
            connection.DeviceId,
            connection.Device?.Hostname ?? connection.Device?.IpAddress ?? "Unknown device",
            connection.Device?.IpAddress ?? "0.0.0.0",
            connection.DestinationIp,
            connection.DestinationDomain,
            connection.Protocol,
            connection.DestinationPort,
            connection.BytesSent,
            connection.BytesReceived,
            connection.FirstSeenUtc,
            connection.LastSeenUtc);
    }
}
