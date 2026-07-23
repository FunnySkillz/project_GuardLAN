using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace GuardLan.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet]
    public Task<DashboardSnapshotDto> Get(CancellationToken cancellationToken)
    {
        return dashboardService.GetSnapshotAsync(cancellationToken);
    }
}
