namespace GuardLan.Application.Models;

public sealed class ConnectionOverviewQueryDto
{
    public int Hours { get; init; } = 24;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 25;

    public string? Protocol { get; init; }

    public string? Search { get; init; }
}
