using Microsoft.Extensions.Configuration;
using System.Globalization;

namespace GuardLan.Infrastructure.Zeek;

internal static class ZeekLogOptionsReader
{
    public static ZeekLogFileOptions Read(
        IConfiguration configuration,
        string sectionName,
        int defaultMaxRecords = 5000)
    {
        var section = configuration.GetSection($"Zeek:{sectionName}");

        return new ZeekLogFileOptions(
            Enabled: ReadBoolean(section, "Enabled", defaultValue: false),
            Path: ReadString(section, "Path", string.Empty),
            CheckpointPath: ReadString(section, "CheckpointPath", string.Empty),
            MaxRecords: Math.Clamp(ReadInteger(section, "MaxRecords", defaultMaxRecords), 1, 50000),
            ReadFromBeginning: ReadBoolean(section, "ReadFromBeginning", defaultValue: true));
    }

    private static string ReadString(IConfiguration section, string key, string defaultValue)
    {
        return string.IsNullOrWhiteSpace(section[key]) ? defaultValue : section[key]!;
    }

    private static int ReadInteger(IConfiguration section, string key, int defaultValue)
    {
        return int.TryParse(section[key], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : defaultValue;
    }

    private static bool ReadBoolean(IConfiguration section, string key, bool defaultValue)
    {
        return bool.TryParse(section[key], out var value) ? value : defaultValue;
    }
}
