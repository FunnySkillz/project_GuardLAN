namespace GuardLan.Application.Models;

public sealed record RegisterDeviceRequest(string DeviceName);

public sealed record RegisterDeviceResponse(Guid DeviceId, string Status);

public sealed record SubmitSyncRequest(Guid DeviceId, SyncUsage Usage);

public sealed record SyncUsage(string AppName, int ForegroundSeconds);

public sealed record SubmitSyncResponse(string Status);

public sealed record MdacRegistrationSummary(Guid DeviceId, string DeviceName, DateTime RegisteredUtc);
