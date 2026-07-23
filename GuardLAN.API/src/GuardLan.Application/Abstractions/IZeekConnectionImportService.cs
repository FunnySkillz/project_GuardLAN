using GuardLan.Application.Models;

namespace GuardLan.Application.Abstractions;

public interface IZeekConnectionImportService
{
    Task<ZeekConnectionImportResultDto> ImportRecentAsync(
        CancellationToken cancellationToken = default);
}
