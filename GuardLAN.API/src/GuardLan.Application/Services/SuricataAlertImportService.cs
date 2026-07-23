using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using GuardLan.Application.Suricata;

namespace GuardLan.Application.Services;

public sealed class SuricataAlertImportService(
    ISuricataAlertSource suricataAlertSource,
    IIdsAlertIngestionService idsAlertIngestionService,
    TimeProvider timeProvider) : ISuricataAlertImportService
{
    public async Task<SuricataAlertImportResultDto> ImportRecentAsync(
        CancellationToken cancellationToken = default)
    {
        var importedAtUtc = timeProvider.GetUtcNow().UtcDateTime;

        if (!suricataAlertSource.IsEnabled)
        {
            return new SuricataAlertImportResultDto(
                suricataAlertSource.SourceName,
                SourceEnabled: false,
                SourceAvailable: false,
                LinesRead: 0,
                RecordsParsed: 0,
                SkippedInvalidSourceRecords: 0,
                Imported: 0,
                SkippedDuplicates: 0,
                SkippedInvalidAlertRecords: 0,
                SkippedUnmatchedDevices: 0,
                MatchedDevices: 0,
                MatchedConnections: 0,
                importedAtUtc,
                $"{suricataAlertSource.SourceName} ingestion is disabled.");
        }

        var sourceResult = await suricataAlertSource.ReadNewAlertsAsync(cancellationToken);
        if (!sourceResult.SourceAvailable)
        {
            return new SuricataAlertImportResultDto(
                suricataAlertSource.SourceName,
                SourceEnabled: true,
                SourceAvailable: false,
                sourceResult.LinesRead,
                sourceResult.RecordsParsed,
                sourceResult.SkippedInvalid,
                Imported: 0,
                SkippedDuplicates: 0,
                SkippedInvalidAlertRecords: 0,
                SkippedUnmatchedDevices: 0,
                MatchedDevices: 0,
                MatchedConnections: 0,
                importedAtUtc,
                sourceResult.Message);
        }

        var ingestionResult = await idsAlertIngestionService.ImportAsync(
            new IdsAlertIngestionBatchDto
            {
                Source = suricataAlertSource.SourceName,
                Records = sourceResult.Records
            },
            cancellationToken);

        if (sourceResult.Checkpoint is not null)
        {
            await suricataAlertSource.SaveCheckpointAsync(sourceResult.Checkpoint, cancellationToken);
        }

        return new SuricataAlertImportResultDto(
            suricataAlertSource.SourceName,
            SourceEnabled: true,
            SourceAvailable: true,
            sourceResult.LinesRead,
            sourceResult.RecordsParsed,
            sourceResult.SkippedInvalid,
            ingestionResult.Imported,
            ingestionResult.SkippedDuplicates,
            ingestionResult.SkippedInvalid,
            ingestionResult.SkippedUnmatchedDevices,
            ingestionResult.MatchedDevices,
            ingestionResult.MatchedConnections,
            importedAtUtc,
            $"Imported {ingestionResult.Imported} Suricata alerts.");
    }
}
