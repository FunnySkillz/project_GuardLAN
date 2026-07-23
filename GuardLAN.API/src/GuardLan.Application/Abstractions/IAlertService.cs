using GuardLan.Application.Models;

namespace GuardLan.Application.Abstractions;

public interface IAlertService
{
    Task<IReadOnlyList<AlertDto>> ListAsync(CancellationToken cancellationToken);

    Task<AlertDto?> ResolveAsync(Guid id, CancellationToken cancellationToken);
}
