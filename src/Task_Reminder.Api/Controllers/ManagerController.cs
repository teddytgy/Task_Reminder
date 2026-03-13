using Microsoft.AspNetCore.Mvc;
using Task_Reminder.Api.Domain.Services;
using Task_Reminder.Api.Security;
using Task_Reminder.Shared;

namespace Task_Reminder.Api.Controllers;

[ApiController]
[Route("api/manager")]
[RequireOfficePermission(OfficePermission.ViewManagerDashboard)]
public sealed class ManagerController(IManagerReportService managerReportService) : ControllerBase
{
    [HttpGet("metrics")]
    public async Task<ActionResult<ManagerMetricsDto>> GetMetricsAsync([FromQuery] ManagerMetricsQuery query, CancellationToken cancellationToken)
        => Ok(await managerReportService.GetMetricsAsync(query, cancellationToken));

    [HttpGet("metrics/export")]
    public async Task<IActionResult> ExportAsync([FromQuery] ManagerMetricsQuery query, CancellationToken cancellationToken)
    {
        var csv = await managerReportService.ExportCsvAsync(query, cancellationToken);
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "task-reminder-manager-report.csv");
    }
}
