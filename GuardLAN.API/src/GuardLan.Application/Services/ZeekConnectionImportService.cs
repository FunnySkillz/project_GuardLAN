using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using GuardLan.Application.Zeek;
using GuardLan.Domain.Enums;

namespace GuardLan.Application.Services;

public sealed class ZeekConnectionImportService(
    IZeekConnectionSource zeekConnectionSource,
    IConnectionIngestionService connectionIngestionService,
    IIntegrationHealthService integrationHealthService,
    TimeProvider timeProvider) : IZeekConnectionImportService
{
    public async Task<ZeekConnectionImportResultDto> ImportRecentAsync(
        CancellationToken cancellationToken = default)
    {
        var importedAtUtc = timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            if (!zeekConnectionSource.IsEnabled)
            {
                return await RecordAndReturnAsync(
                    new ZeekConnectionImportResultDto(
                        zeekConnectionSource.SourceName,
                        SourceEnabled: false,
                        SourceAvailable: false,
                        LinesRead: 0,
                        RecordsParsed: 0,
                        SkippedInvalidSourceRecords: 0,
                        Imported: 0,
                        SkippedDuplicates: 0,
                        SkippedInvalidConnectionRecords: 0,
                        SkippedUnmatchedDevices: 0,
                        MatchedDevices: 0,
                        importedAtUtc,
                        $"{zeekConnectionSource.SourceName} ingestion is disabled."),
                    cancellationToken);
            }

            var sourceResult = await zeekConnectionSource.ReadNewConnectionsAsync(cancellationToken);
            if (!sourceResult.SourceAvailable)
            {
                return await RecordAndReturnAsync(
                    new ZeekConnectionImportResultDto(
                        zeekConnectionSource.SourceName,
                        SourceEnabled: true,
                        SourceAvailable: false,
                        sourceResult.LinesRead,
                        sourceResult.RecordsParsed,
                        sourceResult.SkippedInvalid,
                        Imported: 0,
                        SkippedDuplicates: 0,
                        SkippedInvalidConnectionRecords: 0,
                        SkippedUnmatchedDevices: 0,
                        MatchedDevices: 0,
                        importedAtUtc,
                        sourceResult.Message),
                    cancellationToken);
            }

            if (sourceResult.Records.Count == 0)
            {
                if (sourceResult.Checkpoint is not null)
                {
                    await zeekConnectionSource.SaveCheckpointAsync(sourceResult.Checkpoint, cancellationToken);
                }

                return await RecordAndReturnAsync(
                    new ZeekConnectionImportResultDto(
                        zeekConnectionSource.SourceName,
                        SourceEnabled: true,
                        SourceAvailable: true,
                        sourceResult.LinesRead,
                        sourceResult.RecordsParsed,
                        sourceResult.SkippedInvalid,
                        Imported: 0,
                        SkippedDuplicates: 0,
                        SkippedInvalidConnectionRecords: 0,
                        SkippedUnmatchedDevices: 0,
                        MatchedDevices: 0,
                        importedAtUtc,
                        sourceResult.Message),
                    cancellationToken);
            }

            var ingestionResult = await connectionIngestionService.ImportAsync(
                new ConnectionIngestionBatchDto
                {
                    Source = zeekConnectionSource.SourceName,
                    Records = sourceResult.Records
                },
                cancellationToken);

            if (sourceResult.Checkpoint is not null)
            {
                await zeekConnectionSource.SaveCheckpointAsync(sourceResult.Checkpoint, cancellationToken);
            }

            return await RecordAndReturnAsync(
                new ZeekConnectionImportResultDto(
                    zeekConnectionSource.SourceName,
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
                    importedAtUtc,
                    $"Imported {ingestionResult.Imported} Zeek connection records."),
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await RecordFailureAsync(importedAtUtc, exception.Message, cancellationToken);
            throw;
        }
    }

    private async Task<ZeekConnectionImportResultDto> RecordAndReturnAsync(
        ZeekConnectionImportResultDto result,
        CancellationToken cancellationToken)
    {
        await integrationHealthService.RecordAsync(
            new IntegrationHealthRecord(
                result.Source,
                IntegrationKind.Zeek,
                result.SourceEnabled,
                result.SourceAvailable,
                result.LinesRead,
                result.Imported,
                result.SkippedInvalidSourceRecords +
                result.SkippedInvalidConnectionRecords +
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
                zeekConnectionSource.SourceName,
                IntegrationKind.Zeek,
                zeekConnectionSource.IsEnabled,
                SourceAvailable: false,
                RecordsRead: 0,
                RecordsImported: 0,
                RecordsRejected: 0,
                checkedAtUtc,
                message),
            cancellationToken);
    }
}
