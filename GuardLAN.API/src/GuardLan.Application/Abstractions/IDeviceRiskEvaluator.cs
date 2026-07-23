using GuardLan.Application.Models;
using GuardLan.Domain.Entities;

namespace GuardLan.Application.Abstractions;

public interface IDeviceRiskEvaluator
{
    IReadOnlyDictionary<Guid, DeviceRiskDto> Evaluate(
        IReadOnlyList<NetworkDevice> devices,
        IReadOnlyList<SecurityAlert> alerts,
        IReadOnlyList<DnsQuery> dnsQueries,
        IReadOnlyList<NetworkConnection> connections,
        DateTime nowUtc);
}
