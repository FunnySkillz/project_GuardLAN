using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using GuardLan.Application.Zeek;

namespace GuardLan.Application.Services;

public sealed class ZeekTlsImportService(
    IZeekTlsSource zeekTlsSource,
    ITlsObservationIngestionService tlsObservationIngestionService,
    TimeProvider timeProvider) : IZeekTlsImportService
{
    public async Task<ZeekTlsImportResultDto> ImportRecentAsync(
        CancellationToken cancellationToken = default)
    {
        var importedAtUtc = timeProvider.GetUtcNow().UtcDateTime;

        if (!zeekTlsSource.IsEnabled)
        {
            return new ZeekTlsImportResultDto(
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
                $"{zeekTlsSource.SourceName} ingestion is disabled.");
        }

        var sourceResult = await zeekTlsSource.ReadNewObservationsAsync(cancellationToken);
        if (!sourceResult.SourceAvailable)
        {
            return new ZeekTlsImportResultDto(
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
                sourceResult.Message);
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

        return new ZeekTlsImportResultDto(
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
            $"Imported {ingestionResult.Imported} Zeek TLS observations.");
    }
}
