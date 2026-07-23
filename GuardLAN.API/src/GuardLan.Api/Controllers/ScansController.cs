using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace GuardLan.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ScansController(INetworkScanService networkScanService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<NetworkScanDto>> List(CancellationToken cancellationToken)
    {
        return networkScanService.ListAsync(cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<NetworkScanDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var scan = await networkScanService.GetAsync(id, cancellationToken);

        return scan is null ? NotFound() : Ok(scan);
    }

    [HttpPost]
    public async Task<ActionResult<NetworkScanDto>> Queue(
        QueueNetworkScanCommand command,
        CancellationToken cancellationToken)
    {
        var scan = await networkScanService.QueueAsync(command, cancellationToken);

        return AcceptedAtAction(nameof(Get), new { id = scan.Id }, scan);
    }
}
