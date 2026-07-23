using GuardLan.Domain.Entities;
using GuardLan.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GuardLan.Infrastructure.Persistence.Repositories;

public sealed class NetworkConnectionRepository(GuardLanDbContext dbContext)
    : GenericRepository<NetworkConnection>(dbContext),
      INetworkConnectionRepository
{
    public async Task<IReadOnlyList<NetworkConnection>> GetSinceAsync(
        DateTime sinceUtc,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(connection => connection.LastSeenUtc >= sinceUtc)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NetworkConnection>> GetSinceWithDevicesAsync(
        DateTime sinceUtc,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(connection => connection.Device)
            .Where(connection => connection.LastSeenUtc >= sinceUtc)
            .OrderByDescending(connection => connection.LastSeenUtc)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<NetworkConnectionPage> GetPageSinceWithDevicesAsync(
        NetworkConnectionQuery query,
        CancellationToken cancellationToken = default)
    {
        var filteredQuery = ApplyConnectionFilters(
            DbSet
                .AsNoTracking()
                .Include(connection => connection.Device)
                .Where(connection => connection.LastSeenUtc >= query.SinceUtc),
            query);

        var totalCount = await filteredQuery.CountAsync(cancellationToken);
        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling((double)totalCount / query.PageSize);
        var effectivePage = totalPages == 0 ? 1 : Math.Min(query.Page, totalPages);
        var items = await filteredQuery
            .OrderByDescending(connection => connection.LastSeenUtc)
            .Skip((effectivePage - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToArrayAsync(cancellationToken);

        return new NetworkConnectionPage(
            items,
            effectivePage,
            query.PageSize,
            totalCount);
    }

    public async Task<IReadOnlyList<NetworkConnection>> GetRecentForDeviceAsync(
        Guid deviceId,
        DateTime sinceUtc,
        int limit,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(connection => connection.Device)
            .Where(connection => connection.DeviceId == deviceId && connection.LastSeenUtc >= sinceUtc)
            .OrderByDescending(connection => connection.LastSeenUtc)
            .Take(limit)
            .ToArrayAsync(cancellationToken);
    }

    private static IQueryable<NetworkConnection> ApplyConnectionFilters(
        IQueryable<NetworkConnection> query,
        NetworkConnectionQuery connectionQuery)
    {
        var protocol = connectionQuery.Protocol?.Trim().ToLowerInvariant();

        query = protocol switch
        {
            "tcp" => query.Where(connection => EF.Functions.ILike(connection.Protocol, "tcp")),
            "udp" => query.Where(connection => EF.Functions.ILike(connection.Protocol, "udp")),
            "other" => query.Where(connection =>
                !EF.Functions.ILike(connection.Protocol, "tcp") &&
                !EF.Functions.ILike(connection.Protocol, "udp")),
            _ => query
        };

        var search = connectionQuery.Search?.Trim();
        if (string.IsNullOrWhiteSpace(search))
        {
            return query;
        }

        var searchPattern = $"%{search}%";
        var hasPortSearch = int.TryParse(search, out var portSearch);

        return query.Where(connection =>
            EF.Functions.ILike(connection.DestinationIp, searchPattern) ||
            (connection.DestinationDomain != null &&
             EF.Functions.ILike(connection.DestinationDomain, searchPattern)) ||
            EF.Functions.ILike(connection.Protocol, searchPattern) ||
            (connection.Device != null &&
             (EF.Functions.ILike(connection.Device.IpAddress, searchPattern) ||
              (connection.Device.Hostname != null &&
               EF.Functions.ILike(connection.Device.Hostname, searchPattern)))) ||
            (hasPortSearch && connection.DestinationPort == portSearch));
    }
}
