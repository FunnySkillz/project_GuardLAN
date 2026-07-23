using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using GuardLan.Application.Zeek;
using GuardLan.Domain.Enums;

namespace GuardLan.Application.Services;

public sealed class ZeekTlsImportService(
    IZeekTlsSource zeekTlsSource,
    ITlsObservationIngestionService tlsObservationIngestionService,
    IIntegrationHealthService integrationHealthService,
    TimeProvider timeProvider) : IZeekTlsImportService
{
    public async Task<ZeekTlsImportResultDto> ImportRecentAsync(
        CancellationToken cancellationToken = default)
    {
        var importedAtUtc = timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            if (!zeekTlsSource.IsEnabled)
            {
                return await RecordAndReturnAsync(
                    new ZeekTlsImportResultDto(
                        zeekTlsSource.SourceName,
                        SourceEnabled: false,
                        SourceAvailable: false,
                        LinesRead: 0,
                        RecordsParsed: 0,
                        SkippedInvalidSourceRecords: 0,
                        Imported: 0,
                        SkippedDuplicates: 0,
                        SkippedInvalidTlsRecords: 0,
                        SkippedUnmatchedDevices: 0,
                        MatchedDevices: 0,
                        MatchedConnections: 0,
                        importedAtUtc,
                        $"{zeekTlsSource.SourceName} ingestion is disabled."),
                    cancellationToken);
            }

            var sourceResult = await zeekTlsSource.ReadNewObservationsAsync(cancellationToken);
            if (!sourceResult.SourceAvailable)
            {
                return await RecordAndReturnAsync(
                    new ZeekTlsImportResultDto(
                        zeekTlsSource.SourceName,
                        SourceEnabled: true,
                        SourceAvailable: false,
                        sourceResult.LinesRead,
                        sourceResult.RecordsParsed,
                        sourceResult.SkippedInvalid,
                        Imported: 0,
                        SkippedDuplicates: 0,
                        SkippedInvalidTlsRecords: 0,
                        SkippedUnmatchedDevices: 0,
                        MatchedDevices: 0,
                        MatchedConnections: 0,
                        importedAtUtc,
                        sourceResult.Message),
                    cancellationToken);
            }

            var ingestionResult = await tlsObservationIngestionService.ImportAsync(
                new TlsObservationIngestionBatchDto
                {
                    Source = zeekTlsSource.SourceName,
                    Records = sourceResult.Records
                },
                cancellationToken);

            if (sourceResult.Checkpoint is not null)
            {
                await zeekTlsSource.SaveCheckpointAsync(sourceResult.Checkpoint, cancellationToken);
            }

            return await RecordAndReturnAsync(
                new ZeekTlsImportResultDto(
                    zeekTlsSource.SourceName,
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
                    $"Imported {ingestionResult.Imported} Zeek TLS observations."),
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await RecordFailureAsync(importedAtUtc, exception.Message, cancellationToken);
            throw;
        }
    }

    private async Task<ZeekTlsImportResultDto> RecordAndReturnAsync(
        ZeekTlsImportResultDto result,
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
                result.SkippedInvalidTlsRecords +
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
                zeekTlsSource.SourceName,
                IntegrationKind.Zeek,
                zeekTlsSource.IsEnabled,
                SourceAvailable: false,
                RecordsRead: 0,
                RecordsImported: 0,
                RecordsRejected: 0,
                checkedAtUtc,
                message),
            cancellationToken);
    }
}
