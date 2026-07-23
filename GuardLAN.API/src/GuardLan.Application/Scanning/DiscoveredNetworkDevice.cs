namespace GuardLan.Application.Scanning;

public sealed record DiscoveredNetworkDevice(
    string IpAddress,
    string? MacAddress,
    string? Hostname,
    string? Vendor);
