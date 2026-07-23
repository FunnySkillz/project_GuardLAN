using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace GuardLan.Api.Controllers;

[ApiController]
[Route("api/integrations")]
public sealed class IntegrationsController(
    IIntegrationHealthService integrationHealthService,
    IZeekImportService zeekImportService,
    ISuricataAlertImportService suricataAlertImportService) : ControllerBase
{
    [HttpGet("health")]
    public Task<IntegrationHealthOverviewDto> GetHealth(CancellationToken cancellationToken)
    {
        return integrationHealthService.GetOverviewAsync(cancellationToken);
    }

    [HttpPost("zeek/import")]
    public Task<ZeekImportResultDto> ImportZeek(CancellationToken cancellationToken)
    {
        return zeekImportService.ImportRecentAsync(cancellationToken);
    }

    [HttpPost("suricata/import")]
    public Task<SuricataAlertImportResultDto> ImportSuricata(CancellationToken cancellationToken)
    {
        return suricataAlertImportService.ImportRecentAsync(cancellationToken);
    }
}
