namespace GuardLan.Application.Scanning;

public interface INetworkScanner
{
    Task<IReadOnlyList<DiscoveredNetworkDevice>> ScanAsync(
        string subnet,
        CancellationToken cancellationToken = default);
}
