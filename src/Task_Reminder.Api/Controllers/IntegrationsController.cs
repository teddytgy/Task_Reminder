using Microsoft.AspNetCore.Mvc;
using Task_Reminder.Api.Domain.Services;
using Task_Reminder.Api.Security;
using Task_Reminder.Shared;

namespace Task_Reminder.Api.Controllers;

[ApiController]
[Route("api/integrations")]
[RequireOfficePermission(OfficePermission.ViewAudit)]
public sealed class IntegrationsController(IExternalIntegrationService externalIntegrationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ExternalIntegrationProviderStatusDto>>> ListAsync(CancellationToken cancellationToken)
        => Ok(await externalIntegrationService.ListAsync(cancellationToken));

    [HttpPut("{id:guid}")]
    [RequireOfficePermission(OfficePermission.ManageIntegrations)]
    public async Task<ActionResult<ExternalIntegrationProviderStatusDto>> UpdateAsync(Guid id, [FromBody] UpdateExternalIntegrationProviderRequest request, CancellationToken cancellationToken)
    {
        var result = await externalIntegrationService.UpdateAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:guid}/run")]
    [RequireOfficePermission(OfficePermission.ManageIntegrations)]
    public async Task<ActionResult<ExternalIntegrationProviderStatusDto>> RunAsync(Guid id, [FromBody] RunExternalIntegrationRequest request, CancellationToken cancellationToken)
    {
        var result = await externalIntegrationService.RunAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
