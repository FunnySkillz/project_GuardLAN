using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using GuardLan.Domain.Entities;
using GuardLan.Domain.Repositories;
using System.Net;

namespace GuardLan.Application.Services;

public sealed class TlsObservationIngestionService(
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ITlsObservationIngestionService
{
    private const int MaxSourceLength = 64;
    private const int MaxSourceRecordIdLength = 96;
    private const int MaxServerNameLength = 255;
    private const int MaxVersionLength = 64;
    private const int MaxCipherLength = 128;
    private const int MaxFingerprintLength = 128;
    private const int MaxAlpnLength = 128;
    private static readonly DateTime MinimumAcceptedTimestampUtc =
        DateTime.SpecifyKind(DateTime.UnixEpoch, DateTimeKind.Utc);

    public async Task<TlsObservationIngestionResultDto> ImportAsync(
        TlsObservationIngestionBatchDto batch,
        CancellationToken cancellationToken = default)
    {
        var importedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        var source = NormalizeSource(batch.Source);
        var records = batch.Records ?? [];

        if (records.Count == 0)
        {
            return new TlsObservationIngestionResultDto(
                source,
                RecordsRead: 0,
                Imported: 0,
                SkippedDuplicates: 0,
                SkippedInvalid: 0,
                SkippedUnmatchedDevices: 0,
                MatchedDevices: 0,
                MatchedConnections: 0,
                importedAtUtc,
                "No TLS observation records were supplied.");
        }

        var normalizedRecords = records
            .Select(record => NormalizeRecord(record, importedAtUtc))
            .ToArray();
        var validRecords = normalizedRecords
            .Where(record => record is not null)
            .Cast<NormalizedTlsObservationRecord>()
            .ToArray();
        var skippedInvalid = records.Count - validRecords.Length;

        if (validRecords.Length == 0)
        {
            return new TlsObservationIngestionResultDto(
                source,
                records.Count,
                Imported: 0,
                SkippedDuplicates: 0,
                skippedInvalid,
                SkippedUnmatchedDevices: 0,
                MatchedDevices: 0,
                MatchedConnections: 0,
                importedAtUtc,
                "No valid TLS observation records were supplied.");
        }

        var devices = await unitOfWork.Devices.GetInventoryAsync(cancellationToken);
        var devicesByIp = devices
            .GroupBy(device => NormalizeIpAddress(device.IpAddress), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Key.Length > 0)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var sinceUtc = validRecords.Min(record => record.ObservedUtc).AddMinutes(-5);
        var existingObservations = await unitOfWork.TlsObservations.GetSinceAsync(sinceUtc, cancellationToken);
        var seenKeys = existingObservations
            .Select(BuildKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existingConnections = await unitOfWork.NetworkConnections.GetSinceWithDevicesAsync(
            sinceUtc,
            cancellationToken);
        var connectionsBySourceRecord = existingConnections
            .Select(connection => new
            {
                SourceRecordId = NormalizeText(connection.SourceRecordId, MaxSourceRecordIdLength),
                Connection = connection
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.SourceRecordId))
            .GroupBy(item => item.SourceRecordId!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Connection, StringComparer.OrdinalIgnoreCase);

        var imported = 0;
        var skippedDuplicates = 0;
        var skippedUnmatchedDevices = 0;
        var matchedDevices = 0;
        var matchedConnections = 0;

        foreach (var record in validRecords.OrderBy(record => record.ObservedUtc))
        {
            if (!devicesByIp.TryGetValue(record.SourceIp, out var device))
            {
                skippedUnmatchedDevices++;
                continue;
            }

            matchedDevices++;

            var key = BuildKey(source, record);
            if (!seenKeys.Add(key))
            {
                skippedDuplicates++;
                continue;
            }

            var connection = FindConnection(source, record, device.Id, connectionsBySourceRecord, existingConnections);
            if (connection is not null)
            {
                matchedConnections++;
            }

            await unitOfWork.TlsObservations.AddAsync(
                new TlsObservation
                {
                    Id = Guid.NewGuid(),
                    DeviceId = device.Id,
                    ConnectionId = connection?.Id,
                    Source = source,
                    SourceRecordId = record.SourceRecordId,
                    SourceIp = record.SourceIp,
                    DestinationIp = record.DestinationIp,
                    DestinationPort = record.DestinationPort,
                    ServerName = record.ServerName,
                    Version = record.Version,
                    Cipher = record.Cipher,
                    Ja3 = record.Ja3,
                    Ja3s = record.Ja3s,
                    Alpn = record.Alpn,
                    WasEstablished = record.WasEstablished,
                    ObservedUtc = record.ObservedUtc
                },
                cancellationToken);

            imported++;
        }

        if (imported > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new TlsObservationIngestionResultDto(
            source,
            records.Count,
            imported,
            skippedDuplicates,
            skippedInvalid,
            skippedUnmatchedDevices,
            matchedDevices,
            matchedConnections,
            importedAtUtc,
            $"Imported {imported} TLS observations from {source}.");
    }

    private static NetworkConnection? FindConnection(
        string source,
        NormalizedTlsObservationRecord record,
        Guid deviceId,
        IReadOnlyDictionary<string, NetworkConnection> connectionsBySourceRecord,
        IReadOnlyList<NetworkConnection> existingConnections)
    {
        if (!string.IsNullOrWhiteSpace(record.SourceRecordId))
        {
            var sourceRecordKey = NormalizeText(record.SourceRecordId, MaxSourceRecordIdLength);
            if (!string.IsNullOrWhiteSpace(sourceRecordKey) &&
                connectionsBySourceRecord.TryGetValue(sourceRecordKey, out var sourceRecordMatch))
            {
                return sourceRecordMatch;
            }
        }

        return existingConnections
            .Where(connection =>
                connection.DeviceId == deviceId &&
                string.Equals(connection.DestinationIp, record.DestinationIp, StringComparison.OrdinalIgnoreCase) &&
                connection.DestinationPort == record.DestinationPort &&
                record.ObservedUtc >= connection.FirstSeenUtc.AddMinutes(-5) &&
                record.ObservedUtc <= connection.LastSeenUtc.AddMinutes(5))
            .OrderBy(connection => Math.Abs((connection.FirstSeenUtc - record.ObservedUtc).Ticks))
            .FirstOrDefault();
    }

    private static NormalizedTlsObservationRecord? NormalizeRecord(
        TlsObservationIngestionRecordDto record,
        DateTime importedAtUtc)
    {
        var sourceIp = NormalizeIpAddress(record.SourceIp);
        var destinationIp = NormalizeIpAddress(record.DestinationIp);
        var observedUtc = NormalizeTimestamp(record.ObservedUtc);

        if (sourceIp.Length == 0 ||
            destinationIp.Length == 0 ||
            record.DestinationPort is < 0 or > 65535 ||
            observedUtc <= MinimumAcceptedTimestampUtc ||
            observedUtc > importedAtUtc.AddMinutes(5))
        {
            return null;
        }

        return new NormalizedTlsObservationRecord(
            NormalizeText(record.SourceRecordId, MaxSourceRecordIdLength),
            sourceIp,
            destinationIp,
            record.DestinationPort,
            NormalizeDomain(record.ServerName),
            NormalizeText(record.Version, MaxVersionLength),
            NormalizeText(record.Cipher, MaxCipherLength),
            NormalizeText(record.Ja3, MaxFingerprintLength),
            NormalizeText(record.Ja3s, MaxFingerprintLength),
            NormalizeText(record.Alpn, MaxAlpnLength),
            record.WasEstablished,
            observedUtc);
    }

    private static string NormalizeSource(string? source)
    {
        var normalized = source?.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "TLS";
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

        return normalized.Length <= MaxServerNameLength
            ? normalized
            : normalized[..MaxServerNameLength];
    }

    private static string? NormalizeText(string? value, int maxLength)
    {
        var normalized = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
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

    private static string BuildKey(TlsObservation observation)
    {
        return BuildKey(
            NormalizeSource(observation.Source),
            NormalizeText(observation.SourceRecordId, MaxSourceRecordIdLength),
            NormalizeIpAddress(observation.SourceIp),
            NormalizeIpAddress(observation.DestinationIp),
            observation.DestinationPort,
            NormalizeTimestamp(observation.ObservedUtc),
            NormalizeDomain(observation.ServerName));
    }

    private static string BuildKey(string source, NormalizedTlsObservationRecord record)
    {
        return BuildKey(
            source,
            record.SourceRecordId,
            record.SourceIp,
            record.DestinationIp,
            record.DestinationPort,
            record.ObservedUtc,
            record.ServerName);
    }

    private static string BuildKey(
        string source,
        string? sourceRecordId,
        string sourceIp,
        string destinationIp,
        int? destinationPort,
        DateTime observedUtc,
        string? serverName)
    {
        if (!string.IsNullOrWhiteSpace(sourceRecordId))
        {
            return BuildSourceRecordKey(source, sourceRecordId);
        }

        return string.Join(
            "|",
            sourceIp,
            destinationIp,
            destinationPort?.ToString() ?? string.Empty,
            observedUtc.Ticks,
            serverName ?? string.Empty);
    }

    private static string BuildSourceRecordKey(string source, string? sourceRecordId)
    {
        return string.Join("|", "source-record", source, sourceRecordId ?? string.Empty);
    }

    private sealed record NormalizedTlsObservationRecord(
        string? SourceRecordId,
        string SourceIp,
        string DestinationIp,
        int? DestinationPort,
        string? ServerName,
        string? Version,
        string? Cipher,
        string? Ja3,
        string? Ja3s,
        string? Alpn,
        bool? WasEstablished,
        DateTime ObservedUtc);
}
