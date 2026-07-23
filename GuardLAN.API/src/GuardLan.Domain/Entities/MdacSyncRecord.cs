namespace GuardLan.Domain.Entities;

public sealed class MdacSyncRecord
{
    public Guid Id { get; set; }

    public Guid DeviceId { get; set; }

    public string AppName { get; set; } = string.Empty;

    public int ForegroundSeconds { get; set; }

    public DateTime SyncedUtc { get; set; }
}
