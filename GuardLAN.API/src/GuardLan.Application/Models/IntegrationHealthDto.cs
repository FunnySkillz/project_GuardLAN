using GuardLan.Domain.Entities;
using GuardLan.Domain.Enums;

namespace GuardLan.Application.Models;

public sealed record IntegrationHealthOverviewDto(
    IntegrationHealthSummaryDto Summary,
    IReadOnlyList<IntegrationHealthDto> Sources,
    IReadOnlyList<IntegrationImportRunDto> RecentRuns);

public sealed record IntegrationHealthSummaryDto(
    int TotalSources,
    int HealthySources,
    int WarningSources,
    int UnavailableSources,
    int DisabledSources,
    int StaleSources,
    DateTime? LastCheckedUtc);

public sealed record IntegrationHealthDto(
    Guid Id,
    string Source,
    IntegrationKind Kind,
    IntegrationHealthStatus Status,
    bool SourceEnabled,
    bool SourceAvailable,
    DateTime LastCheckedUtc,
    DateTime? StaleAfterUtc,
    DateTime? LastSuccessUtc,
    DateTime? LastFailureUtc,
    int RecordsRead,
    int RecordsImported,
    int RecordsRejected,
    string Message)
{
    public static IntegrationHealthDto FromEntity(
        IntegrationHealth health,
        IntegrationHealthStatus effectiveStatus)
    {
        return new IntegrationHealthDto(
            health.Id,
            health.Source,
            health.Kind,
            effectiveStatus,
            health.SourceEnabled,
            health.SourceAvailable,
            health.LastCheckedUtc,
            health.StaleAfterUtc,
            health.LastSuccessUtc,
            health.LastFailureUtc,
            health.RecordsRead,
            health.RecordsImported,
            health.RecordsRejected,
            health.Message);
    }
}

public sealed record IntegrationImportRunDto(
    Guid Id,
    string Source,
    IntegrationKind Kind,
    IntegrationHealthStatus Status,
    bool SourceEnabled,
    bool SourceAvailable,
    DateTime CompletedUtc,
    int RecordsRead,
    int RecordsImported,
    int RecordsRejected,
    string Message)
{
    public static IntegrationImportRunDto FromEntity(IntegrationImportRun run)
    {
        return new IntegrationImportRunDto(
            run.Id,
            run.Source,
            run.Kind,
            run.Status,
            run.SourceEnabled,
            run.SourceAvailable,
            run.CompletedUtc,
            run.RecordsRead,
            run.RecordsImported,
            run.RecordsRejected,
            run.Message);
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
