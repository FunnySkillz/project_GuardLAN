using GuardLan.Api.Auth;
using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GuardLan.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/internal/live-updates")]
public sealed class InternalLiveUpdatesController(
    InternalPublisherKeyValidator keyValidator,
    ILiveUpdatePublisher liveUpdatePublisher) : ControllerBase
{
    private const string InternalKeyHeader = "X-GuardLAN-Internal-Key";

    [HttpPost]
    public async Task<IActionResult> Publish(
        LiveUpdateDto update,
        CancellationToken cancellationToken)
    {
        if (!Request.Headers.TryGetValue(InternalKeyHeader, out var suppliedKey) ||
            !keyValidator.IsValid(suppliedKey.ToString()))
        {
            return Unauthorized();
        }

        await liveUpdatePublisher.PublishAsync(update, cancellationToken);

        return Accepted();
    }
}
