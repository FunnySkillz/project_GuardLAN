using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using GuardLan.Application.Options;
using GuardLan.Domain.Entities;
using GuardLan.Domain.Enums;
using GuardLan.Domain.Repositories;
using Microsoft.Extensions.Options;

namespace GuardLan.Application.Services;

public sealed class IntegrationHealthService(
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IOptions<IntegrationHealthOptions> options) : IIntegrationHealthService
{
    private const int RecentRunLimit = 20;

    public async Task<IntegrationHealthOverviewDto> GetOverviewAsync(
        CancellationToken cancellationToken = default)
    {
        var sources = await unitOfWork.IntegrationHealth.GetAllOrderedAsync(cancellationToken);
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var dtos = sources
            .Select(source => IntegrationHealthDto.FromEntity(source, ResolveEffectiveStatus(source, nowUtc)))
            .ToArray();
        var recentRuns = await unitOfWork.IntegrationImportRuns.GetRecentAsync(
            RecentRunLimit,
            cancellationToken);

        return new IntegrationHealthOverviewDto(
            new IntegrationHealthSummaryDto(
                dtos.Length,
                dtos.Count(source => source.Status == IntegrationHealthStatus.Healthy),
                dtos.Count(source => source.Status == IntegrationHealthStatus.Warning),
                dtos.Count(source => source.Status == IntegrationHealthStatus.Unavailable),
                dtos.Count(source => source.Status == IntegrationHealthStatus.Disabled),
                dtos.Count(source => source.Status == IntegrationHealthStatus.Stale),
                dtos.Length == 0 ? null : dtos.Max(source => source.LastCheckedUtc)),
            dtos,
            recentRuns.Select(IntegrationImportRunDto.FromEntity).ToArray());
    }

    public async Task RecordAsync(
        IntegrationHealthRecord record,
        CancellationToken cancellationToken = default)
    {
        var source = NormalizeSource(record.Source);
        if (source.Length == 0)
        {
            return;
        }

        var checkedAtUtc = NormalizeUtc(record.CheckedAtUtc);
        var status = ResolveStatus(record);
        var staleAfterUtc = status is IntegrationHealthStatus.Healthy or IntegrationHealthStatus.Warning
            ? checkedAtUtc.Add(options.Value.ResolveStaleAfter(record.Kind, source))
            : (DateTime?)null;
        var health = await unitOfWork.IntegrationHealth.GetBySourceAsync(source, cancellationToken);

        if (health is null)
        {
            health = new IntegrationHealth
            {
                Id = Guid.NewGuid(),
                Source = source
            };

            await unitOfWork.IntegrationHealth.AddAsync(health, cancellationToken);
        }

        health.Kind = record.Kind;
        health.Status = status;
        health.SourceEnabled = record.SourceEnabled;
        health.SourceAvailable = record.SourceAvailable;
        health.LastCheckedUtc = checkedAtUtc;
        health.StaleAfterUtc = staleAfterUtc;
        health.RecordsRead = Math.Max(0, record.RecordsRead);
        health.RecordsImported = Math.Max(0, record.RecordsImported);
        health.RecordsRejected = Math.Max(0, record.RecordsRejected);
        health.Message = Truncate(record.Message.Trim(), 512);

        if (status is IntegrationHealthStatus.Healthy or IntegrationHealthStatus.Warning)
        {
            health.LastSuccessUtc = checkedAtUtc;
        }
        else if (status == IntegrationHealthStatus.Unavailable)
        {
            health.LastFailureUtc = checkedAtUtc;
        }

        await unitOfWork.IntegrationImportRuns.AddAsync(
            new IntegrationImportRun
            {
                Id = Guid.NewGuid(),
                Source = source,
                Kind = record.Kind,
                Status = status,
                SourceEnabled = record.SourceEnabled,
                SourceAvailable = record.SourceAvailable,
                CompletedUtc = checkedAtUtc,
                RecordsRead = Math.Max(0, record.RecordsRead),
                RecordsImported = Math.Max(0, record.RecordsImported),
                RecordsRejected = Math.Max(0, record.RecordsRejected),
                Message = Truncate(record.Message.Trim(), 512)
            },
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static IntegrationHealthStatus ResolveEffectiveStatus(
        IntegrationHealth health,
        DateTime nowUtc)
    {
        if (health.Status is IntegrationHealthStatus.Disabled or IntegrationHealthStatus.Unavailable)
        {
            return health.Status;
        }

        return health.StaleAfterUtc.HasValue && health.StaleAfterUtc <= nowUtc
            ? IntegrationHealthStatus.Stale
            : health.Status;
    }

    private static IntegrationHealthStatus ResolveStatus(IntegrationHealthRecord record)
    {
        if (!record.SourceEnabled)
        {
            return IntegrationHealthStatus.Disabled;
        }

        if (!record.SourceAvailable)
        {
            return IntegrationHealthStatus.Unavailable;
        }

        return record.RecordsRejected > 0
            ? IntegrationHealthStatus.Warning
            : IntegrationHealthStatus.Healthy;
    }

    private static string NormalizeSource(string source)
    {
        return Truncate(source.Trim(), 96);
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Local => value.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _ => value.ToUniversalTime()
        };
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
