using GuardLan.Domain.Entities;
using GuardLan.Domain.Enums;

namespace GuardLan.Application.Models;

public sealed record IntegrationHealthOverviewDto(
    IntegrationHealthSummaryDto Summary,
    IReadOnlyList<IntegrationHealthDto> Sources);

public sealed record IntegrationHealthSummaryDto(
    int TotalSources,
    int HealthySources,
    int WarningSources,
    int UnavailableSources,
    int DisabledSources,
    DateTime? LastCheckedUtc);

public sealed record IntegrationHealthDto(
    Guid Id,
    string Source,
    IntegrationKind Kind,
    IntegrationHealthStatus Status,
    bool SourceEnabled,
    bool SourceAvailable,
    DateTime LastCheckedUtc,
    DateTime? LastSuccessUtc,
    DateTime? LastFailureUtc,
    int RecordsRead,
    int RecordsImported,
    int RecordsRejected,
    string Message)
{
    public static IntegrationHealthDto FromEntity(IntegrationHealth health)
    {
        return new IntegrationHealthDto(
            health.Id,
            health.Source,
            health.Kind,
            health.Status,
            health.SourceEnabled,
            health.SourceAvailable,
            health.LastCheckedUtc,
            health.LastSuccessUtc,
            health.LastFailureUtc,
            health.RecordsRead,
            health.RecordsImported,
            health.RecordsRejected,
            health.Message);
    }
}

public sealed record IntegrationHealthRecord(
    string Source,
    IntegrationKind Kind,
    bool SourceEnabled,
    bool SourceAvailable,
    int RecordsRead,
    int RecordsImported,
    int RecordsRejected,
    DateTime CheckedAtUtc,
    string Message);
