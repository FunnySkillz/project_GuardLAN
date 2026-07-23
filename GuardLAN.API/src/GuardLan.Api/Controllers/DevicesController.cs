using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace GuardLan.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class DevicesController(IDeviceService deviceService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<DeviceDto>> List(CancellationToken cancellationToken)
    {
        return deviceService.ListAsync(cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DeviceDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var device = await deviceService.GetAsync(id, cancellationToken);

        return device is null ? NotFound() : Ok(device);
    }

    [HttpGet("{id:guid}/evidence")]
    public async Task<ActionResult<DeviceEvidenceDto>> GetEvidence(Guid id, CancellationToken cancellationToken)
    {
        var evidence = await deviceService.GetEvidenceAsync(id, cancellationToken);

        return evidence is null ? NotFound() : Ok(evidence);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<DeviceDto>> Update(
        Guid id,
        UpdateDeviceCommand command,
        CancellationToken cancellationToken)
    {
        var device = await deviceService.UpdateAsync(id, command, cancellationToken);

        return device is null ? NotFound() : Ok(device);
    }
}
