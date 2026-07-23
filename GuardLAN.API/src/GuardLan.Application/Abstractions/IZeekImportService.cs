using GuardLan.Application.Models;

namespace GuardLan.Application.Abstractions;

public interface IZeekImportService
{
    Task<ZeekImportResultDto> ImportRecentAsync(
        CancellationToken cancellationToken = default);
}
