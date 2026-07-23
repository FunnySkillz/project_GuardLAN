using GuardLan.Domain.Entities;

namespace GuardLan.Domain.Repositories;

public sealed record NetworkConnectionPage(
    IReadOnlyList<NetworkConnection> Items,
    int Page,
    int PageSize,
    int TotalCount);
