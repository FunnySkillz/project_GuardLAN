using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using GuardLan.Domain.Entities;
using GuardLan.Domain.Enums;
using GuardLan.Domain.Repositories;

namespace GuardLan.Application.Services;

public sealed class IntegrationHealthService(IUnitOfWork unitOfWork) : IIntegrationHealthService
{
    public async Task<IntegrationHealthOverviewDto> GetOverviewAsync(
        CancellationToken cancellationToken = default)
    {
        var sources = await unitOfWork.IntegrationHealth.GetAllOrderedAsync(cancellationToken);
        var dtos = sources.Select(IntegrationHealthDto.FromEntity).ToArray();

        return new IntegrationHealthOverviewDto(
            new IntegrationHealthSummaryDto(
                dtos.Length,
                dtos.Count(source => source.Status == IntegrationHealthStatus.Healthy),
                dtos.Count(source => source.Status == IntegrationHealthStatus.Warning),
                dtos.Count(source => source.Status == IntegrationHealthStatus.Unavailable),
                dtos.Count(source => source.Status == IntegrationHealthStatus.Disabled),
                dtos.Length == 0 ? null : dtos.Max(source => source.LastCheckedUtc)),
            dtos);
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

        await unitOfWork.SaveChangesAsync(cancellationToken);
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
