using Microsoft.AspNetCore.Mvc;
using Task_Reminder.Api.Domain.Services;
using Task_Reminder.Api.Security;
using Task_Reminder.Shared;

namespace Task_Reminder.Api.Controllers;

[ApiController]
[Route("api/office-settings")]
public sealed class OfficeSettingsController(IOfficeSettingsService officeSettingsService) : ControllerBase
{
    [HttpGet]
    [RequireOfficePermission(OfficePermission.ViewManagerDashboard)]
    public async Task<ActionResult<OfficeSettingsDto>> GetAsync(CancellationToken cancellationToken)
        => Ok(await officeSettingsService.GetAsync(cancellationToken));

    [HttpPut]
    [RequireOfficePermission(OfficePermission.ManageOfficeSettings)]
    public async Task<ActionResult<OfficeSettingsDto>> UpdateAsync([FromBody] UpdateOfficeSettingsRequest request, CancellationToken cancellationToken)
        => Ok(await officeSettingsService.UpdateAsync(request, cancellationToken));
}
