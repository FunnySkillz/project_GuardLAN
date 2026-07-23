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
        [FromQuery] ConnectionOverviewQueryDto query,
        CancellationToken cancellationToken = default)
    {
        return connectionService.GetOverviewAsync(query, cancellationToken);
    }
}
