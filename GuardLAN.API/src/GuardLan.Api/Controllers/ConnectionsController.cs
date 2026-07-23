using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace GuardLan.Api.Controllers;

[ApiController]
[Route("api/connections")]
public sealed class ConnectionsController(IConnectionService connectionService) : ControllerBase
{
    [HttpGet("overview")]
    public Task<ConnectionOverviewDto> GetOverview(
        [FromQuery] int hours = 24,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        return connectionService.GetOverviewAsync(hours, limit, cancellationToken);
    }
}
