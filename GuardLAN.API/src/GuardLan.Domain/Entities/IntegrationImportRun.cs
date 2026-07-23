using GuardLan.Domain.Enums;

namespace GuardLan.Domain.Entities;

public class IntegrationImportRun
{
    public Guid Id { get; set; }

    public string Source { get; set; } = null!;

    public IntegrationKind Kind { get; set; }

    public IntegrationHealthStatus Status { get; set; }

    public bool SourceEnabled { get; set; }

    public bool SourceAvailable { get; set; }

    public DateTime CompletedUtc { get; set; }

    public int RecordsRead { get; set; }

    public int RecordsImported { get; set; }

    public int RecordsRejected { get; set; }

    public string Message { get; set; } = null!;
}
