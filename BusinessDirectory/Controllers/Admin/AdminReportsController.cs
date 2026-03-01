using BusinessDirectory.Application.Dtos.Admin;
using BusinessDirectory.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessDirectory.API.Controllers.Admin;

[ApiController]
[Route("api/admin/reports")]
[Authorize(Roles = "Admin")]
public sealed class AdminReportsController : ControllerBase
{
    private readonly IAdminReportService _reportService;

    public AdminReportsController(IAdminReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<AdminReportSummaryDto>> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var summary = await _reportService.GetSummaryAsync(cancellationToken);
        return Ok(summary);
    }
}
