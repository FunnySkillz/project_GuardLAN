using GuardLan.Application.Abstractions;
using GuardLan.Application.Dns;
using GuardLan.Application.Models;
using GuardLan.Domain.Entities;
using GuardLan.Domain.Repositories;
using System.Net;

namespace GuardLan.Application.Services;

public sealed class DnsRecordIngestionService(
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IDnsRecordIngestionService
{
    private static readonly DateTime MinimumAcceptedTimestampUtc =
        DateTime.SpecifyKind(DateTime.UnixEpoch, DateTimeKind.Utc);

    public async Task<DnsIngestionResultDto> ImportAsync(
        string sourceName,
        bool sourceEnabled,
        IReadOnlyList<DnsIngestionRecord> sourceRecords,
        CancellationToken cancellationToken = default)
    {
        var importedAtUtc = timeProvider.GetUtcNow().UtcDateTime;

        if (!sourceEnabled)
        {
            return new DnsIngestionResultDto(
                sourceName,
                SourceEnabled: false,
                RecordsRead: 0,
                Imported: 0,
                SkippedDuplicates: 0,
                SkippedInvalid: 0,
                MatchedDevices: 0,
                importedAtUtc,
                $"{sourceName} ingestion is disabled.");
        }

        if (sourceRecords.Count == 0)
        {
            return new DnsIngestionResultDto(
                sourceName,
                SourceEnabled: true,
                RecordsRead: 0,
                Imported: 0,
                SkippedDuplicates: 0,
                SkippedInvalid: 0,
                MatchedDevices: 0,
                importedAtUtc,
                $"No DNS records were returned by {sourceName}.");
        }

        var normalizedRecords = sourceRecords
            .Select(record => NormalizeRecord(record, importedAtUtc))
            .ToArray();
        var validRecords = normalizedRecords
            .Where(record => record is not null)
            .Cast<NormalizedDnsRecord>()
            .ToArray();

        var skippedInvalid = sourceRecords.Count - validRecords.Length;
        if (validRecords.Length == 0)
        {
            return new DnsIngestionResultDto(
                sourceName,
                SourceEnabled: true,
                sourceRecords.Count,
                Imported: 0,
                SkippedDuplicates: 0,
                skippedInvalid,
                MatchedDevices: 0,
                importedAtUtc,
                $"No valid DNS records were returned by {sourceName}.");
        }

        var sinceUtc = validRecords.Min(record => record.TimestampUtc).AddSeconds(-1);
        var existingQueries = await unitOfWork.DnsQueries.GetSinceAsync(sinceUtc, cancellationToken);
        var seenKeys = existingQueries
            .Select(query => BuildKey(query.ClientIp, query.Domain, NormalizeTimestamp(query.TimestampUtc)))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var devices = await unitOfWork.Devices.GetInventoryAsync(cancellationToken);
        var devicesByIp = devices
            .GroupBy(device => NormalizeClientIp(device.IpAddress), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var imported = 0;
        var skippedDuplicates = 0;
        var matchedDevices = 0;

        foreach (var record in validRecords.OrderBy(record => record.TimestampUtc))
        {
            var key = BuildKey(record.ClientIp, record.Domain, record.TimestampUtc);
            if (!seenKeys.Add(key))
            {
                skippedDuplicates++;
                continue;
            }

            devicesByIp.TryGetValue(record.ClientIp, out var device);
            if (device is not null)
            {
                matchedDevices++;
            }

            await unitOfWork.DnsQueries.AddAsync(
                new DnsQuery
                {
                    Id = Guid.NewGuid(),
                    DeviceId = device?.Id,
                    ClientIp = record.ClientIp,
                    Domain = record.Domain,
                    WasBlocked = record.WasBlocked,
                    TimestampUtc = record.TimestampUtc
                },
                cancellationToken);

            imported++;
        }

        if (imported > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new DnsIngestionResultDto(
            sourceName,
            SourceEnabled: true,
            sourceRecords.Count,
            imported,
            skippedDuplicates,
            skippedInvalid,
            matchedDevices,
            importedAtUtc,
            $"Imported {imported} DNS records from {sourceName}.");
    }

    private static NormalizedDnsRecord? NormalizeRecord(DnsIngestionRecord record, DateTime importedAtUtc)
    {
        var clientIp = NormalizeClientIp(record.ClientIp);
        var domain = NormalizeDomain(record.Domain);
        var timestampUtc = NormalizeTimestamp(record.TimestampUtc);

        if (clientIp.Length == 0 ||
            clientIp.Length > 64 ||
            domain.Length == 0 ||
            domain.Length > 255 ||
            timestampUtc <= MinimumAcceptedTimestampUtc ||
            timestampUtc > importedAtUtc.AddMinutes(5))
        {
            return null;
        }

        return new NormalizedDnsRecord(clientIp, domain, record.WasBlocked, timestampUtc);
    }

    private static string NormalizeClientIp(string clientIp)
    {
        var normalized = clientIp.Trim().Trim('[', ']');

        return IPAddress.TryParse(normalized, out var address)
            ? address.ToString()
            : string.Empty;
    }

    private static string NormalizeDomain(string domain)
    {
        return domain
            .Trim()
            .TrimEnd('.')
            .ToLowerInvariant();
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

    private static string BuildKey(string clientIp, string domain, DateTime timestampUtc)
    {
        return $"{clientIp}|{domain}|{timestampUtc.Ticks}";
    }

    private sealed record NormalizedDnsRecord(
        string ClientIp,
        string Domain,
        bool WasBlocked,
        DateTime TimestampUtc);
}
