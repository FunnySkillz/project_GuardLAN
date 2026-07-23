using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GuardLan.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
public sealed class MdacAdminController(IMdacService mdacService) : ControllerBase
{
    [HttpGet("registrations")]
    public async Task<ActionResult<IReadOnlyList<MdacRegistrationSummary>>> ListRegistrations(CancellationToken cancellationToken)
    {
        var registrations = await mdacService.ListRegistrationsAsync(cancellationToken);
        return Ok(registrations);
    }
}
