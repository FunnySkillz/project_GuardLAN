using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace GuardLan.Api.Controllers;

[ApiController]
[Route("api/dns")]
public sealed class DnsController(IDnsService dnsService) : ControllerBase
{
    [HttpGet("overview")]
    public Task<DnsOverviewDto> GetOverview(
        [FromQuery] int hours = 24,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        return dnsService.GetOverviewAsync(hours, limit, cancellationToken);
    }
}
