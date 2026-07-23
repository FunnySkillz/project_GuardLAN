using GuardLan.Application.Models;

namespace GuardLan.Application.Abstractions;

public interface IZeekTlsImportService
{
    Task<ZeekTlsImportResultDto> ImportRecentAsync(
        CancellationToken cancellationToken = default);
}
