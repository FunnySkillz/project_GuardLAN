using GuardLan.Domain.Enums;

namespace GuardLan.Application.Options;

public sealed class IntegrationHealthOptions
{
    public const string SectionName = "IntegrationHealth";
    public const int DefaultStaleAfterMinutesValue = 15;
    private const int MinimumStaleAfterMinutes = 1;
    private const int MaximumStaleAfterMinutes = 7 * 24 * 60;

    public int DefaultStaleAfterMinutes { get; init; } = DefaultStaleAfterMinutesValue;

    public Dictionary<string, int> StaleAfterMinutesByKind { get; init; } = [];

    public Dictionary<string, int> StaleAfterMinutesBySource { get; init; } = [];

    public TimeSpan ResolveStaleAfter(IntegrationKind kind, string source)
    {
        var minutes = ResolveConfiguredMinutes(kind, source);

        return TimeSpan.FromMinutes(Math.Clamp(
            minutes,
            MinimumStaleAfterMinutes,
            MaximumStaleAfterMinutes));
    }

    private int ResolveConfiguredMinutes(IntegrationKind kind, string source)
    {
        if (TryGetPositiveValue(StaleAfterMinutesBySource, source, out var sourceMinutes))
        {
            return sourceMinutes;
        }

        if (TryGetPositiveValue(StaleAfterMinutesByKind, kind.ToString(), out var kindMinutes))
        {
            return kindMinutes;
        }

        return DefaultStaleAfterMinutes > 0
            ? DefaultStaleAfterMinutes
            : DefaultStaleAfterMinutesValue;
    }

    private static bool TryGetPositiveValue(
        IReadOnlyDictionary<string, int> values,
        string key,
        out int value)
    {
        foreach (var pair in values)
        {
            if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase) && pair.Value > 0)
            {
                value = pair.Value;
                return true;
            }
        }

        value = 0;
        return false;
    }
}
