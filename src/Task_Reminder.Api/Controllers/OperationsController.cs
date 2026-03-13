using Microsoft.AspNetCore.Mvc;
using Task_Reminder.Api.Domain.Services;
using Task_Reminder.Api.Security;
using Task_Reminder.Shared;

namespace Task_Reminder.Api.Controllers;

[ApiController]
[Route("api/operations")]
[RequireOfficePermission(OfficePermission.ViewManagerDashboard)]
public sealed class OperationsController(IOperationsBoardService operationsBoardService) : ControllerBase
{
    [HttpGet("board")]
    public async Task<ActionResult<OperationsBoardDto>> GetBoardAsync(CancellationToken cancellationToken)
        => Ok(await operationsBoardService.GetBoardAsync(cancellationToken));

    [HttpGet("workload")]
    public async Task<ActionResult<IReadOnlyList<UserWorkloadDto>>> GetWorkloadAsync(CancellationToken cancellationToken)
        => Ok(await operationsBoardService.GetWorkloadAsync(cancellationToken));

    [HttpGet("activity/{userId:guid}")]
    public async Task<ActionResult<IReadOnlyList<UserActivityTimelineItemDto>>> GetUserActivityAsync(Guid userId, CancellationToken cancellationToken)
        => Ok(await operationsBoardService.GetUserActivityAsync(userId, cancellationToken));

    [HttpGet("kpis")]
    public async Task<ActionResult<OperationsKpiDto>> GetKpisAsync([FromQuery] ManagerMetricsQuery query, CancellationToken cancellationToken)
        => Ok(await operationsBoardService.GetKpisAsync(query, cancellationToken));

    [HttpGet("export/{exportType}")]
    public async Task<IActionResult> ExportAsync(string exportType, [FromQuery] ManagerMetricsQuery query, CancellationToken cancellationToken)
    {
        var csv = await operationsBoardService.ExportOperationsCsvAsync(exportType, query, cancellationToken);
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"task-reminder-{exportType}.csv");
    }
}
