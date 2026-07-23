using System.Globalization;

namespace GuardLan.Infrastructure.Zeek;

internal static class ZeekValueParser
{
    public static bool TryParseTimestamp(string value, out DateTime timestampUtc)
    {
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) &&
            seconds > 0)
        {
            try
            {
                timestampUtc = DateTime.UnixEpoch.AddSeconds(seconds);
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
            }
        }

        timestampUtc = default;
        return false;
    }

    public static bool TryParsePort(string value, out int? port)
    {
        if (value.Length == 0)
        {
            port = null;
            return true;
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) &&
            parsed is >= 0 and <= 65535)
        {
            port = parsed;
            return true;
        }

        port = null;
        return false;
    }

    public static double? ParseNullableDouble(string value)
    {
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    public static long? ParseNullableLong(string value)
    {
        return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) &&
               parsed >= 0
            ? parsed
            : null;
    }

    public static bool ParseBoolean(string value)
    {
        if (bool.TryParse(value, out var parsed))
        {
            return parsed;
        }

        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) &&
               number != 0;
    }
}
