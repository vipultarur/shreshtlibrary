using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/v1/reports")]
    [Authorize(Roles = "admin,super_admin,sub_super_admin")]
    public class ReportsController : ControllerBase
    {
        private readonly WebApplication1.Services.IReportsService _reportsService;

        public ReportsController(WebApplication1.Services.IReportsService reportsService)
        {
            _reportsService = reportsService;
        }

        [HttpGet("attendance")]
        [ProducesResponseType(typeof(WebApplication1.Models.Responses.ApiResponse<object>), 200)]
        public async Task<IActionResult> ReportsAttendanceAsync([FromQuery] int page = 1, [FromQuery] int page_size = 10, CancellationToken ct = default)
        {
            page_size = Math.Clamp(page_size, 1, 100);
            page = Math.Max(1, page);
            var result = await _reportsService.GetAttendanceReportAsync(page, page_size, ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("payments")]
        [ProducesResponseType(typeof(WebApplication1.Models.Responses.ApiResponse<object>), 200)]
        public async Task<IActionResult> ReportsPaymentsAsync([FromQuery] int page = 1, [FromQuery] int page_size = 10, CancellationToken ct = default)
        {
            page_size = Math.Clamp(page_size, 1, 100);
            page = Math.Max(1, page);
            var result = await _reportsService.GetPaymentsReportAsync(page, page_size, ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("students")]
        [ProducesResponseType(typeof(WebApplication1.Models.Responses.ApiResponse<object>), 200)]
        public async Task<IActionResult> ReportsStudentsAsync([FromQuery] int page = 1, [FromQuery] int page_size = 10, CancellationToken ct = default)
        {
            page_size = Math.Clamp(page_size, 1, 100);
            page = Math.Max(1, page);
            var result = await _reportsService.GetStudentsReportAsync(page, page_size, ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("memberships")]
        [ProducesResponseType(typeof(WebApplication1.Models.Responses.ApiResponse<object>), 200)]
        public async Task<IActionResult> ReportsMembershipsAsync([FromQuery] int page = 1, [FromQuery] int page_size = 10, CancellationToken ct = default)
        {
            page_size = Math.Clamp(page_size, 1, 100);
            page = Math.Max(1, page);
            var result = await _reportsService.GetMembershipsReportAsync(page, page_size, ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("daily-summary")]
        [ProducesResponseType(typeof(WebApplication1.Models.Responses.ApiResponse<object>), 200)]
        public async Task<IActionResult> ReportsDailySummaryAsync(CancellationToken ct)
        {
            var result = await _reportsService.GetDailySummaryAsync(ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("seats")]
        [ProducesResponseType(typeof(WebApplication1.Models.Responses.ApiResponse<object>), 200)]
        public async Task<IActionResult> ReportsSeatsAsync(CancellationToken ct)
        {
            var result = await _reportsService.GetSeatsReportAsync(ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("export/{kind}")]
        [ProducesResponseType(typeof(WebApplication1.Models.Responses.ApiResponse<object>), 200)]
        public async Task<IActionResult> ReportsExportAsync(string kind, CancellationToken ct)
        {
            var allowedKinds = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
                { "attendance", "payments", "students", "memberships", "seats", "daily-summary" };
            if (!allowedKinds.Contains(kind))
                return BadRequest(WebApplication1.Models.Responses.ApiResponse<object>
                    .Fail($"Invalid report kind '{kind}'."));

            var fileBytes = await _reportsService.ExportReportCsvAsync(kind, ct);
            if (fileBytes == null) return NotFound();
            return File(fileBytes, "text/csv", $"{kind}_report.csv");
        }
    }
}
