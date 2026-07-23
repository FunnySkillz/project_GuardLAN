using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using GuardLan.Domain.Entities;
using GuardLan.Domain.Enums;
using GuardLan.Domain.Repositories;
using System.Net;

namespace GuardLan.Application.Services;

public sealed class IdsAlertIngestionService(
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILiveUpdatePublisher liveUpdatePublisher) : IIdsAlertIngestionService
{
    private const int MaxSourceLength = 64;
    private const int MaxSourceRecordIdLength = 128;
    private const int MaxProtocolLength = 32;
    private const int MaxTypeLength = 96;
    private const int MaxMessageLength = 512;
    private const int MaxEvidenceLength = 1024;
    private static readonly DateTime MinimumAcceptedTimestampUtc =
        DateTime.SpecifyKind(DateTime.UnixEpoch, DateTimeKind.Utc);

    public async Task<IdsAlertIngestionResultDto> ImportAsync(
        IdsAlertIngestionBatchDto batch,
        CancellationToken cancellationToken = default)
    {
        var importedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        var source = NormalizeSource(batch.Source);
        var records = batch.Records ?? [];

        if (records.Count == 0)
        {
            return new IdsAlertIngestionResultDto(
                source,
                RecordsRead: 0,
                Imported: 0,
                SkippedDuplicates: 0,
                SkippedInvalid: 0,
                SkippedUnmatchedDevices: 0,
                MatchedDevices: 0,
                MatchedConnections: 0,
                importedAtUtc,
                "No IDS alert records were supplied.");
        }

        var normalizedRecords = records
            .Select(record => NormalizeRecord(record, importedAtUtc))
            .ToArray();
        var validRecords = normalizedRecords
            .Where(record => record is not null)
            .Cast<NormalizedIdsAlertRecord>()
            .ToArray();
        var skippedInvalid = records.Count - validRecords.Length;

        if (validRecords.Length == 0)
        {
            return new IdsAlertIngestionResultDto(
                source,
                records.Count,
                Imported: 0,
                SkippedDuplicates: 0,
                skippedInvalid,
                SkippedUnmatchedDevices: 0,
                MatchedDevices: 0,
                MatchedConnections: 0,
                importedAtUtc,
                "No valid IDS alert records were supplied.");
        }

        var devices = await unitOfWork.Devices.GetInventoryAsync(cancellationToken);
        var devicesByIp = devices
            .GroupBy(device => NormalizeIpAddress(device.IpAddress), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Key.Length > 0)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var sinceUtc = validRecords.Min(record => record.TimestampUtc).AddMinutes(-5);
        var existingAlerts = await unitOfWork.SecurityAlerts.GetSinceAsync(sinceUtc, cancellationToken);
        var seenKeys = existingAlerts
            .Select(BuildKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existingConnections = await unitOfWork.NetworkConnections.GetSinceWithDevicesAsync(
            sinceUtc,
            cancellationToken);

        var imported = 0;
        var skippedDuplicates = 0;
        var skippedUnmatchedDevices = 0;
        var matchedDevices = 0;
        var matchedConnections = 0;
        Guid? firstAlertId = null;
        string? firstAlertMessage = null;

        foreach (var record in validRecords.OrderBy(record => record.TimestampUtc))
        {
            var key = BuildKey(source, record);
            if (!seenKeys.Add(key))
            {
                skippedDuplicates++;
                continue;
            }

            var device = MatchDevice(record, devicesByIp);
            if (device is null)
            {
                skippedUnmatchedDevices++;
                continue;
            }

            matchedDevices++;

            var connection = MatchConnection(record, device.Id, existingConnections);
            if (connection is not null)
            {
                matchedConnections++;
            }

            var alertId = Guid.NewGuid();
            var alert = new SecurityAlert
            {
                Id = alertId,
                DeviceId = device.Id,
                ConnectionId = connection?.Id,
                Source = source,
                SourceRecordId = record.SourceRecordId,
                SourceIp = record.SourceIp,
                DestinationIp = record.DestinationIp,
                DestinationPort = record.DestinationPort,
                Protocol = record.Protocol,
                Severity = MapSeverity(record.Severity),
                Type = "IdsAlert",
                Message = BuildMessage(record),
                CreatedUtc = record.TimestampUtc,
                EvidenceSummary = record.EvidenceSummary
            };

            alert.History.Add(
                new SecurityAlertHistory
                {
                    Id = Guid.NewGuid(),
                    SecurityAlertId = alertId,
                    EventType = "Imported",
                    Description = $"Imported from {source}.",
                    CreatedUtc = importedAtUtc
                });

            await unitOfWork.SecurityAlerts.AddAsync(alert, cancellationToken);
            firstAlertId ??= alert.Id;
            firstAlertMessage ??= alert.Message;
            imported++;
        }

        if (imported > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await liveUpdatePublisher.PublishAsync(
                new LiveUpdateDto(
                    LiveUpdateTypes.NewAlert,
                    imported == 1
                        ? firstAlertMessage ?? $"Imported 1 IDS alert from {source}."
                        : $"Imported {imported} IDS alerts from {source}.",
                    importedAtUtc,
                    AlertId: firstAlertId,
                    Source: source,
                    Count: imported),
                cancellationToken);
        }

        return new IdsAlertIngestionResultDto(
            source,
            records.Count,
            imported,
            skippedDuplicates,
            skippedInvalid,
            skippedUnmatchedDevices,
            matchedDevices,
            matchedConnections,
            importedAtUtc,
            $"Imported {imported} IDS alerts from {source}.");
    }

    private static NormalizedIdsAlertRecord? NormalizeRecord(
        IdsAlertIngestionRecordDto record,
        DateTime importedAtUtc)
    {
        var sourceIp = NormalizeOptionalIpAddress(record.SourceIp);
        var destinationIp = NormalizeOptionalIpAddress(record.DestinationIp);
        var signature = NormalizeRequiredText(record.Signature, MaxMessageLength);
        var timestampUtc = NormalizeTimestamp(record.TimestampUtc);

        if ((sourceIp is null && destinationIp is null) ||
            signature.Length == 0 ||
            record.SourcePort is < 0 or > 65535 ||
            record.DestinationPort is < 0 or > 65535 ||
            timestampUtc <= MinimumAcceptedTimestampUtc ||
            timestampUtc > importedAtUtc.AddMinutes(5))
        {
            return null;
        }

        return new NormalizedIdsAlertRecord(
            NormalizeText(record.SourceRecordId, MaxSourceRecordIdLength),
            sourceIp,
            destinationIp,
            record.SourcePort,
            record.DestinationPort,
            NormalizeText(record.Protocol, MaxProtocolLength)?.ToUpperInvariant(),
            signature,
            NormalizeText(record.Category, MaxTypeLength),
            record.Severity,
            NormalizeText(record.Action, MaxTypeLength),
            NormalizeText(record.EvidenceSummary, MaxEvidenceLength),
            timestampUtc);
    }

    private static NetworkDevice? MatchDevice(
        NormalizedIdsAlertRecord record,
        IReadOnlyDictionary<string, NetworkDevice> devicesByIp)
    {
        if (record.SourceIp is not null &&
            devicesByIp.TryGetValue(record.SourceIp, out var sourceDevice))
        {
            return sourceDevice;
        }

        if (record.DestinationIp is not null &&
            devicesByIp.TryGetValue(record.DestinationIp, out var destinationDevice))
        {
            return destinationDevice;
        }

        return null;
    }

    private static NetworkConnection? MatchConnection(
        NormalizedIdsAlertRecord record,
        Guid deviceId,
        IReadOnlyList<NetworkConnection> connections)
    {
        return connections
            .Where(connection =>
                connection.DeviceId == deviceId &&
                MatchesEndpoint(record, connection) &&
                record.TimestampUtc >= connection.FirstSeenUtc.AddMinutes(-5) &&
                record.TimestampUtc <= connection.LastSeenUtc.AddMinutes(5))
            .OrderBy(connection => Math.Abs((connection.FirstSeenUtc - record.TimestampUtc).Ticks))
            .FirstOrDefault();
    }

    private static bool MatchesEndpoint(NormalizedIdsAlertRecord record, NetworkConnection connection)
    {
        if (record.DestinationIp is not null &&
            string.Equals(connection.DestinationIp, record.DestinationIp, StringComparison.OrdinalIgnoreCase) &&
            (!record.DestinationPort.HasValue || connection.DestinationPort == record.DestinationPort))
        {
            return true;
        }

        return record.SourceIp is not null &&
               string.Equals(connection.DestinationIp, record.SourceIp, StringComparison.OrdinalIgnoreCase) &&
               (!record.SourcePort.HasValue || connection.DestinationPort == record.SourcePort);
    }

    private static AlertSeverity MapSeverity(int? severity)
    {
        return severity switch
        {
            <= 1 => AlertSeverity.Critical,
            2 => AlertSeverity.High,
            3 => AlertSeverity.Medium,
            _ => AlertSeverity.Low
        };
    }

    private static string BuildMessage(NormalizedIdsAlertRecord record)
    {
        var category = string.IsNullOrWhiteSpace(record.Category) ? "IDS" : record.Category;

        return TrimToLength($"{category}: {record.Signature}", MaxMessageLength);
    }

    private static string NormalizeSource(string? source)
    {
        var normalized = source?.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "IDS";
        }

        return TrimToLength(normalized, MaxSourceLength);
    }

    private static string? NormalizeOptionalIpAddress(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim().Trim('[', ']');

        return IPAddress.TryParse(normalized, out var address)
            ? address.ToString()
            : null;
    }

    private static string NormalizeIpAddress(string value)
    {
        var normalized = value.Trim().Trim('[', ']');

        return IPAddress.TryParse(normalized, out var address)
            ? address.ToString()
            : string.Empty;
    }

    private static string NormalizeRequiredText(string? value, int maxLength)
    {
        return NormalizeText(value, maxLength) ?? string.Empty;
    }

    private static string? NormalizeText(string? value, int maxLength)
    {
        var normalized = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return TrimToLength(normalized, maxLength);
    }

    private static string TrimToLength(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
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

    private static string BuildKey(SecurityAlert alert)
    {
        return BuildKey(
            NormalizeSource(alert.Source),
            NormalizeText(alert.SourceRecordId, MaxSourceRecordIdLength),
            NormalizeOptionalIpAddress(alert.SourceIp),
            NormalizeOptionalIpAddress(alert.DestinationIp),
            alert.DestinationPort,
            NormalizeRequiredText(alert.Message, MaxMessageLength),
            NormalizeTimestamp(alert.CreatedUtc));
    }

    private static string BuildKey(string source, NormalizedIdsAlertRecord record)
    {
        return BuildKey(
            source,
            record.SourceRecordId,
            record.SourceIp,
            record.DestinationIp,
            record.DestinationPort,
            BuildMessage(record),
            record.TimestampUtc);
    }

    private static string BuildKey(
        string source,
        string? sourceRecordId,
        string? sourceIp,
        string? destinationIp,
        int? destinationPort,
        string message,
        DateTime timestampUtc)
    {
        if (!string.IsNullOrWhiteSpace(sourceRecordId))
        {
            return string.Join("|", "source-record", source, sourceRecordId);
        }

        return string.Join(
            "|",
            source,
            sourceIp ?? string.Empty,
            destinationIp ?? string.Empty,
            destinationPort?.ToString() ?? string.Empty,
            message,
            timestampUtc.Ticks);
    }

    private sealed record NormalizedIdsAlertRecord(
        string? SourceRecordId,
        string? SourceIp,
        string? DestinationIp,
        int? SourcePort,
        int? DestinationPort,
        string? Protocol,
        string Signature,
        string? Category,
        int? Severity,
        string? Action,
        string? EvidenceSummary,
        DateTime TimestampUtc);
}
