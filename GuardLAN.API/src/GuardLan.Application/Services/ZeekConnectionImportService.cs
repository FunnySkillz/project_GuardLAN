using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using GuardLan.Application.Zeek;

namespace GuardLan.Application.Services;

public sealed class ZeekConnectionImportService(
    IZeekConnectionSource zeekConnectionSource,
    IConnectionIngestionService connectionIngestionService,
    TimeProvider timeProvider) : IZeekConnectionImportService
{
    public async Task<ZeekConnectionImportResultDto> ImportRecentAsync(
        CancellationToken cancellationToken = default)
    {
        var importedAtUtc = timeProvider.GetUtcNow().UtcDateTime;

        if (!zeekConnectionSource.IsEnabled)
        {
            return new ZeekConnectionImportResultDto(
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
                $"{zeekConnectionSource.SourceName} ingestion is disabled.");
        }

        var sourceResult = await zeekConnectionSource.ReadNewConnectionsAsync(cancellationToken);
        if (!sourceResult.SourceAvailable)
        {
            return new ZeekConnectionImportResultDto(
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
                sourceResult.Message);
        }

        if (sourceResult.Records.Count == 0)
        {
            if (sourceResult.Checkpoint is not null)
            {
                await zeekConnectionSource.SaveCheckpointAsync(sourceResult.Checkpoint, cancellationToken);
            }

            return new ZeekConnectionImportResultDto(
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
                sourceResult.Message);
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

        return new ZeekConnectionImportResultDto(
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
            $"Imported {ingestionResult.Imported} Zeek connection records.");
    }
}
