using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace GuardLan.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AlertsController(IAlertService alertService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<AlertDto>> List(CancellationToken cancellationToken)
    {
        return alertService.ListAsync(cancellationToken);
    }

    [HttpPatch("{id:guid}/resolve")]
    public async Task<ActionResult<AlertDto>> Resolve(Guid id, CancellationToken cancellationToken)
    {
        var alert = await alertService.ResolveAsync(id, cancellationToken);

        return alert is null ? NotFound() : Ok(alert);
    }
}
