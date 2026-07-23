namespace GuardLan.Api.Auth;

public sealed class GuardLanAuthOptions
{
    public const string SectionName = "GuardLanAuth";
    public const string DevelopmentPassword = "guardlan";
    public const string DevelopmentInternalPublisherKey = "guardlan-dev-internal";

    public string AdminUsername { get; init; } = "guardlan";

    public string AdminPassword { get; init; } = string.Empty;

    public string AdminPasswordHash { get; init; } = string.Empty;

    public string CookieName { get; init; } = "GuardLAN.Session";

    public int SessionHours { get; init; } = 8;

    public bool RequireSecureCookies { get; init; }

    public string InternalPublisherKey { get; init; } = string.Empty;
}
