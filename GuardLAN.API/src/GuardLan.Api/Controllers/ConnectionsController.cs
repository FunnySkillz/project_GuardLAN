using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace GuardLan.Api.Controllers;

[ApiController]
[Route("api/connections")]
public sealed class ConnectionsController(
    IConnectionService connectionService,
    IConnectionIngestionService connectionIngestionService) : ControllerBase
{
    [HttpGet("overview")]
    public Task<ConnectionOverviewDto> GetOverview(
        [FromQuery] ConnectionOverviewQueryDto query,
        CancellationToken cancellationToken = default)
    {
        return connectionService.GetOverviewAsync(query, cancellationToken);
    }

    [HttpPost("import")]
    public Task<ConnectionIngestionResultDto> Import(
        [FromBody] ConnectionIngestionBatchDto batch,
        CancellationToken cancellationToken)
    {
        return connectionIngestionService.ImportAsync(batch, cancellationToken);
    }
}
