using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using GuardLan.Domain.Entities;
using GuardLan.Domain.Repositories;
using System.Net;

namespace GuardLan.Application.Services;

public sealed class ConnectionIngestionService(
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IConnectionIngestionService
{
    private const int MaxSourceLength = 64;
    private const int MaxProtocolLength = 32;
    private const int MaxDomainLength = 255;
    private static readonly DateTime MinimumAcceptedTimestampUtc =
        DateTime.SpecifyKind(DateTime.UnixEpoch, DateTimeKind.Utc);

    public async Task<ConnectionIngestionResultDto> ImportAsync(
        ConnectionIngestionBatchDto batch,
        CancellationToken cancellationToken = default)
    {
        var importedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        var source = NormalizeSource(batch.Source);
        var records = batch.Records ?? [];

        if (records.Count == 0)
        {
            return new ConnectionIngestionResultDto(
                source,
                RecordsRead: 0,
                Imported: 0,
                SkippedDuplicates: 0,
                SkippedInvalid: 0,
                SkippedUnmatchedDevices: 0,
                MatchedDevices: 0,
                importedAtUtc,
                "No connection records were supplied.");
        }

        var normalizedRecords = records
            .Select(record => NormalizeRecord(record, importedAtUtc))
            .ToArray();
        var validRecords = normalizedRecords
            .Where(record => record is not null)
            .Cast<NormalizedConnectionRecord>()
            .ToArray();
        var skippedInvalid = records.Count - validRecords.Length;

        if (validRecords.Length == 0)
        {
            return new ConnectionIngestionResultDto(
                source,
                records.Count,
                Imported: 0,
                SkippedDuplicates: 0,
                skippedInvalid,
                SkippedUnmatchedDevices: 0,
                MatchedDevices: 0,
                importedAtUtc,
                "No valid connection records were supplied.");
        }

        var devices = await unitOfWork.Devices.GetInventoryAsync(cancellationToken);
        var devicesByIp = devices
            .GroupBy(device => NormalizeIpAddress(device.IpAddress), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Key.Length > 0)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var sinceUtc = validRecords.Min(record => record.FirstSeenUtc).AddSeconds(-1);
        var existingConnections = await unitOfWork.NetworkConnections.GetSinceAsync(
            sinceUtc,
            cancellationToken);
        var seenKeys = existingConnections
            .Select(BuildKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var imported = 0;
        var skippedDuplicates = 0;
        var skippedUnmatchedDevices = 0;
        var matchedDevices = 0;

        foreach (var record in validRecords.OrderBy(record => record.FirstSeenUtc))
        {
            if (!devicesByIp.TryGetValue(record.SourceIp, out var device))
            {
                skippedUnmatchedDevices++;
                continue;
            }

            matchedDevices++;

            var key = BuildKey(
                device.Id,
                record.DestinationIp,
                record.DestinationDomain,
                record.Protocol,
                record.DestinationPort,
                record.FirstSeenUtc,
                record.LastSeenUtc);

            if (!seenKeys.Add(key))
            {
                skippedDuplicates++;
                continue;
            }

            await unitOfWork.NetworkConnections.AddAsync(
                new NetworkConnection
                {
                    Id = Guid.NewGuid(),
                    DeviceId = device.Id,
                    DestinationIp = record.DestinationIp,
                    DestinationDomain = record.DestinationDomain,
                    Protocol = record.Protocol,
                    DestinationPort = record.DestinationPort,
                    BytesSent = record.BytesSent,
                    BytesReceived = record.BytesReceived,
                    FirstSeenUtc = record.FirstSeenUtc,
                    LastSeenUtc = record.LastSeenUtc
                },
                cancellationToken);

            imported++;
        }

        if (imported > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new ConnectionIngestionResultDto(
            source,
            records.Count,
            imported,
            skippedDuplicates,
            skippedInvalid,
            skippedUnmatchedDevices,
            matchedDevices,
            importedAtUtc,
            $"Imported {imported} connection records from {source}.");
    }

    private static NormalizedConnectionRecord? NormalizeRecord(
        ConnectionIngestionRecordDto record,
        DateTime importedAtUtc)
    {
        var sourceIp = NormalizeIpAddress(record.SourceIp);
        var destinationIp = NormalizeIpAddress(record.DestinationIp);
        var destinationDomain = NormalizeDomain(record.DestinationDomain);
        var protocol = NormalizeProtocol(record.Protocol);
        var firstSeenUtc = NormalizeTimestamp(record.StartedUtc);
        var lastSeenUtc = NormalizeTimestamp(record.EndedUtc);

        if (sourceIp.Length == 0 ||
            destinationIp.Length == 0 ||
            protocol.Length == 0 ||
            record.DestinationPort is < 0 or > 65535 ||
            record.BytesSent < 0 ||
            record.BytesReceived < 0 ||
            firstSeenUtc <= MinimumAcceptedTimestampUtc ||
            lastSeenUtc <= MinimumAcceptedTimestampUtc ||
            firstSeenUtc > lastSeenUtc ||
            firstSeenUtc > importedAtUtc.AddMinutes(5) ||
            lastSeenUtc > importedAtUtc.AddMinutes(5))
        {
            return null;
        }

        return new NormalizedConnectionRecord(
            sourceIp,
            destinationIp,
            destinationDomain,
            protocol,
            record.DestinationPort,
            record.BytesSent,
            record.BytesReceived,
            firstSeenUtc,
            lastSeenUtc);
    }

    private static string NormalizeSource(string? source)
    {
        var normalized = source?.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "Normalized";
        }

        return normalized.Length <= MaxSourceLength ? normalized : normalized[..MaxSourceLength];
    }

    private static string NormalizeIpAddress(string value)
    {
        var normalized = value.Trim().Trim('[', ']');

        return IPAddress.TryParse(normalized, out var address)
            ? address.ToString()
            : string.Empty;
    }

    private static string? NormalizeDomain(string? domain)
    {
        var normalized = domain?
            .Trim()
            .TrimEnd('.')
            .ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return normalized.Length <= MaxDomainLength ? normalized : normalized[..MaxDomainLength];
    }

    private static string NormalizeProtocol(string protocol)
    {
        var normalized = protocol.Trim().ToUpperInvariant();

        if (normalized.Length == 0)
        {
            return string.Empty;
        }

        return normalized.Length <= MaxProtocolLength ? normalized : normalized[..MaxProtocolLength];
    }

    private static DateTime NormalizeTimestamp(DateTime timestampUtc)
    {
        var normalized = timestampUtc.Kind switch
        {
            DateTimeKind.Local => timestampUtc.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(timestampUtc, DateTimeKind.Utc),
            _ => timestampUtc.ToUniversalTime()
        };

        return new DateTime(
            normalized.Ticks - normalized.Ticks % TimeSpan.TicksPerSecond,
            DateTimeKind.Utc);
    }

    private static string BuildKey(NetworkConnection connection)
    {
        return BuildKey(
            connection.DeviceId,
            NormalizeIpAddress(connection.DestinationIp),
            NormalizeDomain(connection.DestinationDomain),
            NormalizeProtocol(connection.Protocol),
            connection.DestinationPort,
            NormalizeTimestamp(connection.FirstSeenUtc),
            NormalizeTimestamp(connection.LastSeenUtc));
    }

    private static string BuildKey(
        Guid deviceId,
        string destinationIp,
        string? destinationDomain,
        string protocol,
        int? destinationPort,
        DateTime firstSeenUtc,
        DateTime lastSeenUtc)
    {
        return string.Join(
            "|",
            deviceId,
            destinationIp,
            destinationDomain ?? string.Empty,
            protocol,
            destinationPort?.ToString() ?? string.Empty,
            firstSeenUtc.Ticks,
            lastSeenUtc.Ticks);
    }

    private sealed record NormalizedConnectionRecord(
        string SourceIp,
        string DestinationIp,
        string? DestinationDomain,
        string Protocol,
        int? DestinationPort,
        long BytesSent,
        long BytesReceived,
        DateTime FirstSeenUtc,
        DateTime LastSeenUtc);
}
