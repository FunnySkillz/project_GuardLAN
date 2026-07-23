namespace GuardLan.Application.Models;

public enum DeviceRiskLevel
{
    Normal = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public sealed record DeviceRiskDto(
    DeviceRiskLevel Level,
    int Score,
    IReadOnlyList<string> Reasons)
{
    public static DeviceRiskDto Normal { get; } = new(
        DeviceRiskLevel.Normal,
        Score: 0,
        ["No recent risk evidence."]);
}
