namespace GuardLan.Application.Models;

public sealed record LiveUpdateDto(
    string Type,
    string Message,
    DateTime CreatedUtc,
    Guid? DeviceId = null,
    Guid? AlertId = null,
    Guid? ScanRunId = null,
    string? Status = null,
    string? Source = null,
    int? Count = null);

public static class LiveUpdateTypes
{
    public const string AlertResolved = "alertResolved";
    public const string DeviceStatusChanged = "deviceStatusChanged";
    public const string DnsIngestionCompleted = "dnsIngestionCompleted";
    public const string NewAlert = "newAlert";
    public const string NewDevice = "newDevice";
    public const string ScanCompleted = "scanCompleted";
    public const string ScanFailed = "scanFailed";
    public const string ScanQueued = "scanQueued";
}

public static class LiveUpdateMethods
{
    public const string ClientUpdate = "liveUpdate";
}
