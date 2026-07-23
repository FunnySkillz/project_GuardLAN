namespace GuardLan.Application.Dns;

public interface IDnsQuerySource
{
    string SourceName { get; }

    bool IsEnabled { get; }

    Task<IReadOnlyList<DnsIngestionRecord>> GetRecentQueriesAsync(
        CancellationToken cancellationToken = default);
}
