using GuardLan.Application.Abstractions;
using GuardLan.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace GuardLan.Api.Controllers;

[ApiController]
[Route("api/integrations")]
public sealed class IntegrationsController(
    IZeekImportService zeekImportService,
    ISuricataAlertImportService suricataAlertImportService) : ControllerBase
{
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
