namespace GuardLan.Domain.Entities;

public sealed class MdacRegistration
{
    public Guid Id { get; set; }

    public Guid DeviceId { get; set; }

    public string DeviceName { get; set; } = string.Empty;

    public DateTime RegisteredUtc { get; set; }
}
