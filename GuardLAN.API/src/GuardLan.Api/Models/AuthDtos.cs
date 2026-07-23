namespace GuardLan.Api.Models;

public sealed record LoginRequestDto(string Username, string Password);

public sealed record AuthSessionDto(
    bool Authenticated,
    string? Username,
    DateTime? ExpiresUtc)
{
    public static AuthSessionDto Anonymous { get; } = new(false, null, null);
}
