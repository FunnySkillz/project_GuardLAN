using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GuardLan.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
public sealed class MdacController(IMdacService mdacService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<RegisterDeviceResponse>> Register(
        [FromBody] RegisterDeviceRequest request,
        CancellationToken cancellationToken)
    {
        var response = await mdacService.RegisterAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("sync")]
    public async Task<ActionResult<SubmitSyncResponse>> SubmitSync(
        [FromBody] SubmitSyncRequest request,
        CancellationToken cancellationToken)
    {
        var response = await mdacService.SubmitSyncAsync(request, cancellationToken);
        return Ok(response);
    }
}
