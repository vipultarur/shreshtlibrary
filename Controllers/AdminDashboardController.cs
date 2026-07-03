using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using WebApplication1.DTOs.Admin;
using WebApplication1.Models.Responses;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/v1/admin")]
    [Authorize(Roles = "admin,super_admin")]
    public class AdminDashboardController : ControllerBase
    {
        private readonly IAdminDashboardService _adminDashboardService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<AdminDashboardController> _logger;

        public AdminDashboardController(
            IAdminDashboardService adminDashboardService,
            ICurrentUserService currentUserService,
            ILogger<AdminDashboardController> logger)
        {
            _adminDashboardService = adminDashboardService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetAdminProfileAsync(CancellationToken ct)
        {
            var userId = _currentUserService.GetUserId();
            if (userId == null)
            {
                _logger.LogWarning("Unauthorized access attempt to GetAdminProfileAsync.");
                return Unauthorized(new { message = "Unauthorized access" });
            }

            var profile = await _adminDashboardService.GetAdminProfileAsync(userId.Value, ct);
            
            if (profile == null) return NotFound(new { message = "Admin profile not found" });

            return Ok(ApiResponse<object>.Ok(profile));
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateAdminProfileAsync([FromForm] AdminProfileUpdateDto request, CancellationToken ct)
        {
            var userId = _currentUserService.GetUserId();
            if (userId == null) return Unauthorized();

            var updatedProfile = await _adminDashboardService.UpdateAdminProfileAsync(
                userId.Value, request, Request.Scheme, Request.Host.ToString(), ct);

            if (updatedProfile == null) return NotFound(new { message = "Admin not found" });

            return Ok(ApiResponse<object>.Ok(updatedProfile));
        }



        [HttpGet("dashboard/stats")]
        [HttpGet("dashboard/stats/")]
        [HttpGet("/api/v1/dashboard/stats")]
        [HttpGet("/api/v1/dashboard/stats/")]
        [HttpGet("dashboard/stats/{section}")]
        [HttpGet("dashboard/stats/{section}/")]
        [HttpGet("/api/v1/dashboard/stats/{section}")]
        [HttpGet("/api/v1/dashboard/stats/{section}/")]
        public async Task<IActionResult> GetStatsOverviewAsync(string section = "overview", CancellationToken ct = default)
        {
            var stats = await _adminDashboardService.GetStatsOverviewAsync(section, ct);
            return Ok(ApiResponse<object>.Ok(stats));
        }

        [HttpGet("dashboard/charts")]
        [HttpGet("dashboard/charts/")]
        [HttpGet("/api/v1/dashboard/charts")]
        [HttpGet("/api/v1/dashboard/charts/")]
        public async Task<IActionResult> GetDashboardChartsAsync([FromQuery] string range = "month", CancellationToken ct = default)
        {
            var charts = await _adminDashboardService.GetDashboardChartsAsync(range, ct);
            return Ok(ApiResponse<object>.Ok(charts));
        }
        
        [HttpGet("dashboard/charts/attendance/overview")]
        [HttpGet("dashboard/charts/attendance/overview/")]
        [HttpGet("/api/v1/dashboard/charts/attendance/overview")]
        [HttpGet("/api/v1/dashboard/charts/attendance/overview/")]
        public async Task<IActionResult> GetAttendanceOverviewChartsAsync(CancellationToken ct)
        {
            var data = await _adminDashboardService.GetAttendanceOverviewChartsAsync(ct);
            return Ok(ApiResponse<object>.Ok(data));
        }

        [HttpGet("dashboard/charts/revenue/overview")]
        [HttpGet("dashboard/charts/revenue/overview/")]
        [HttpGet("/api/v1/dashboard/charts/revenue/overview")]
        [HttpGet("/api/v1/dashboard/charts/revenue/overview/")]
        public async Task<IActionResult> GetRevenueOverviewChartsAsync(CancellationToken ct)
        {
            var data = await _adminDashboardService.GetRevenueOverviewChartsAsync(ct);
            return Ok(ApiResponse<object>.Ok(data));
        }

        [HttpGet("dashboard/charts/students/overview")]
        [HttpGet("dashboard/charts/students/overview/")]
        [HttpGet("/api/v1/dashboard/charts/students/overview")]
        [HttpGet("/api/v1/dashboard/charts/students/overview/")]
        public async Task<IActionResult> GetStudentsOverviewChartsAsync(CancellationToken ct)
        {
            var data = await _adminDashboardService.GetStudentsOverviewChartsAsync(ct);
            return Ok(ApiResponse<object>.Ok(data));
        }

        [HttpGet("dashboard/charts/memberships/overview")]
        [HttpGet("dashboard/charts/memberships/overview/")]
        [HttpGet("/api/v1/dashboard/charts/memberships/overview")]
        [HttpGet("/api/v1/dashboard/charts/memberships/overview/")]
        public async Task<IActionResult> GetMembershipsOverviewChartsAsync(CancellationToken ct)
        {
            var data = await _adminDashboardService.GetMembershipsOverviewChartsAsync(ct);
            return Ok(ApiResponse<object>.Ok(data));
        }



        [HttpGet("dashboard/alerts")]
        [HttpGet("dashboard/alerts/")]
        [HttpGet("/api/v1/dashboard/alerts")]
        [HttpGet("/api/v1/dashboard/alerts/")]
        public async Task<IActionResult> GetDashboardAlertsAsync(CancellationToken ct)
        {
            var data = await _adminDashboardService.GetDashboardAlertsAsync(ct);
            return Ok(ApiResponse<object>.Ok(data));
        }

        [HttpGet("dashboard/activity/recent")]
        [HttpGet("dashboard/activity/recent/")]
        [HttpGet("/api/v1/dashboard/activity/recent")]
        [HttpGet("/api/v1/dashboard/activity/recent/")]
        public async Task<IActionResult> GetRecentActivityAsync(CancellationToken ct)
        {
            var data = await _adminDashboardService.GetRecentActivityAsync(ct);
            return Ok(ApiResponse<object>.Ok(data));
        }
    }
}
