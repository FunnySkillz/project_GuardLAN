using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

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
    public async Task<ActionResult<AlertDto>> Resolve(
        Guid id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] AlertReviewCommand? command,
        CancellationToken cancellationToken)
    {
        var alert = await alertService.ResolveAsync(id, command ?? EmptyCommand, cancellationToken);

        return alert is null ? NotFound() : Ok(alert);
    }

    [HttpPatch("{id:guid}/review")]
    public async Task<ActionResult<AlertDto>> MarkReviewed(
        Guid id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] AlertReviewCommand? command,
        CancellationToken cancellationToken)
    {
        var alert = await alertService.MarkReviewedAsync(id, command ?? EmptyCommand, cancellationToken);

        return alert is null ? NotFound() : Ok(alert);
    }

    [HttpPatch("{id:guid}/false-positive")]
    public async Task<ActionResult<AlertDto>> MarkFalsePositive(
        Guid id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] AlertReviewCommand? command,
        CancellationToken cancellationToken)
    {
        var alert = await alertService.MarkFalsePositiveAsync(id, command ?? EmptyCommand, cancellationToken);

        return alert is null ? NotFound() : Ok(alert);
    }

    [HttpPatch("{id:guid}/suppress")]
    public async Task<ActionResult<AlertDto>> Suppress(
        Guid id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] AlertReviewCommand? command,
        CancellationToken cancellationToken)
    {
        var alert = await alertService.SuppressAsync(id, command ?? EmptyCommand, cancellationToken);

        return alert is null ? NotFound() : Ok(alert);
    }

    [HttpPatch("{id:guid}/reopen")]
    public async Task<ActionResult<AlertDto>> Reopen(
        Guid id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] AlertReviewCommand? command,
        CancellationToken cancellationToken)
    {
        var alert = await alertService.ReopenAsync(id, command ?? EmptyCommand, cancellationToken);

        return alert is null ? NotFound() : Ok(alert);
    }

    private static readonly AlertReviewCommand EmptyCommand = new(null);
}
