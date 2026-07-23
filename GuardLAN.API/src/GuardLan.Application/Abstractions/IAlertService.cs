using GuardLan.Application.Models;

namespace GuardLan.Application.Abstractions;

public interface IAlertService
{
    Task<IReadOnlyList<AlertDto>> ListAsync(CancellationToken cancellationToken);

    Task<AlertDto?> MarkReviewedAsync(
        Guid id,
        AlertReviewCommand command,
        CancellationToken cancellationToken);

    Task<AlertDto?> ResolveAsync(
        Guid id,
        AlertReviewCommand command,
        CancellationToken cancellationToken);

    Task<AlertDto?> MarkFalsePositiveAsync(
        Guid id,
        AlertReviewCommand command,
        CancellationToken cancellationToken);

    Task<AlertDto?> SuppressAsync(
        Guid id,
        AlertReviewCommand command,
        CancellationToken cancellationToken);

    Task<AlertDto?> ReopenAsync(
        Guid id,
        AlertReviewCommand command,
        CancellationToken cancellationToken);
}
