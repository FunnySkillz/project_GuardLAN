using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using GuardLan.Application.Suricata;
using GuardLan.Domain.Enums;

namespace GuardLan.Application.Services;

public sealed class SuricataAlertImportService(
    ISuricataAlertSource suricataAlertSource,
    IIdsAlertIngestionService idsAlertIngestionService,
    IIntegrationHealthService integrationHealthService,
    TimeProvider timeProvider) : ISuricataAlertImportService
{
    public async Task<SuricataAlertImportResultDto> ImportRecentAsync(
        CancellationToken cancellationToken = default)
    {
        var importedAtUtc = timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            if (!suricataAlertSource.IsEnabled)
            {
                return await RecordAndReturnAsync(
                    new SuricataAlertImportResultDto(
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
                        $"{suricataAlertSource.SourceName} ingestion is disabled."),
                    cancellationToken);
            }

            var sourceResult = await suricataAlertSource.ReadNewAlertsAsync(cancellationToken);
            if (!sourceResult.SourceAvailable)
            {
                return await RecordAndReturnAsync(
                    new SuricataAlertImportResultDto(
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
                        sourceResult.Message),
                    cancellationToken);
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

            return await RecordAndReturnAsync(
                new SuricataAlertImportResultDto(
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
                    $"Imported {ingestionResult.Imported} Suricata alerts."),
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await RecordFailureAsync(importedAtUtc, exception.Message, cancellationToken);
            throw;
        }
    }

    private async Task<SuricataAlertImportResultDto> RecordAndReturnAsync(
        SuricataAlertImportResultDto result,
        CancellationToken cancellationToken)
    {
        await integrationHealthService.RecordAsync(
            new IntegrationHealthRecord(
                result.Source,
                IntegrationKind.Suricata,
                result.SourceEnabled,
                result.SourceAvailable,
                result.LinesRead,
                result.Imported,
                result.SkippedInvalidSourceRecords +
                result.SkippedInvalidAlertRecords +
                result.SkippedUnmatchedDevices,
                result.ImportedAtUtc,
                result.Message),
            cancellationToken);

        return result;
    }

    private Task RecordFailureAsync(
        DateTime checkedAtUtc,
        string message,
        CancellationToken cancellationToken)
    {
        return integrationHealthService.RecordAsync(
            new IntegrationHealthRecord(
                suricataAlertSource.SourceName,
                IntegrationKind.Suricata,
                suricataAlertSource.IsEnabled,
                SourceAvailable: false,
                RecordsRead: 0,
                RecordsImported: 0,
                RecordsRejected: 0,
                checkedAtUtc,
                message),
            cancellationToken);
    }
}
