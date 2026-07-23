namespace GuardLan.Domain.Repositories;

public sealed record NetworkConnectionQuery(
    DateTime SinceUtc,
    int Page,
    int PageSize,
    string? Protocol,
    string? Search);
