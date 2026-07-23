using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace GuardLan.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ScansController(INetworkScanService networkScanService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<NetworkScanDto>> Queue(
        QueueNetworkScanCommand command,
        CancellationToken cancellationToken)
    {
        var scan = await networkScanService.QueueAsync(command, cancellationToken);

        return AcceptedAtAction(nameof(Queue), new { id = scan.Id }, scan);
    }
}
