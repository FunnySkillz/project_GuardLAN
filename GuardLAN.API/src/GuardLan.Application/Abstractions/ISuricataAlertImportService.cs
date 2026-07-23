using GuardLan.Application.Models;

namespace GuardLan.Application.Abstractions;

public interface ISuricataAlertImportService
{
    Task<SuricataAlertImportResultDto> ImportRecentAsync(
        CancellationToken cancellationToken = default);
}
