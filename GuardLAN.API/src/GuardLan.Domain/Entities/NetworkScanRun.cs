using GuardLan.Domain.Enums;

namespace GuardLan.Domain.Entities;

public class NetworkScanRun
{
    public Guid Id { get; set; }

    public string Subnet { get; set; } = null!;

    public NetworkScanStatus Status { get; set; }

    public DateTime RequestedUtc { get; set; }

    public DateTime? StartedUtc { get; set; }

    public DateTime? FinishedUtc { get; set; }

    public int DevicesDiscovered { get; set; }

    public string? Notes { get; set; }
}
