using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models.DTOs.Admin;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/v1/superadmin")]
    [Authorize(Roles = "admin,super_admin,sub_super_admin")]
    public class SuperAdminController : ControllerBase
    {


        private readonly WebApplication1.Services.ISuperAdminService _superAdminService;

        public SuperAdminController(WebApplication1.Services.ISuperAdminService superAdminService)
        {
            _superAdminService = superAdminService;
        }

        [WebApplication1.Utils.AuthorizePermission(WebApplication1.Utils.Permissions.AdminManagement.Create)]
        [HttpPost("admins")]
        public async Task<IActionResult> AdminsAddAsync([FromBody] AdminPayload payload, System.Threading.CancellationToken ct)
        {
            var result = await _superAdminService.AddAdminAsync(payload, ct);
            if (!result.Success) return BadRequest(WebApplication1.Models.Responses.ApiResponse<object>.Fail(result.Message, result.Errors));
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [WebApplication1.Utils.AuthorizePermission(WebApplication1.Utils.Permissions.AdminManagement.Edit)]
        [HttpPut("admins/{pk}")]
        public async Task<IActionResult> AdminsUpdateAsync(long pk, [FromBody] AdminPayload payload, System.Threading.CancellationToken ct)
        {
            var result = await _superAdminService.UpdateAdminAsync(pk, payload, ct);
            if (!result.Success) return BadRequest(WebApplication1.Models.Responses.ApiResponse<object>.Fail(result.Message, result.Errors));
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [WebApplication1.Utils.AuthorizePermission(WebApplication1.Utils.Permissions.AdminManagement.View)]
        [HttpGet("admins")]
        public async Task<IActionResult> AdminsListAsync(System.Threading.CancellationToken ct)
        {
            var result = await _superAdminService.GetAdminsListAsync(ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [WebApplication1.Utils.AuthorizePermission(WebApplication1.Utils.Permissions.AdminManagement.View)]
        [HttpGet("admins/{pk}")]
        public async Task<IActionResult> AdminDetailAsync(long pk, System.Threading.CancellationToken ct)
        {
            var result = await _superAdminService.GetAdminDetailAsync(pk, ct);
            if (result.IsNotFound) return NotFound(WebApplication1.Models.Responses.ApiResponse<object>.Fail(result.Message));
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [WebApplication1.Utils.AuthorizePermission(WebApplication1.Utils.Permissions.AdminManagement.Delete)]
        [HttpDelete("admins/{pk}/remove")]
        public async Task<IActionResult> AdminRemoveAsync(long pk, System.Threading.CancellationToken ct)
        {
            var result = await _superAdminService.RemoveAdminAsync(pk, ct);
            if (result.IsNotFound) return NotFound(WebApplication1.Models.Responses.ApiResponse<object>.Fail(result.Message));
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(new { }, "Admin removed"));
        }

        [WebApplication1.Utils.AuthorizePermission(WebApplication1.Utils.Permissions.AdminManagement.Suspend)]
        [HttpPost("admins/{pk}/deactivate")]
        public async Task<IActionResult> AdminDeactivateAsync(long pk, System.Threading.CancellationToken ct)
        {
            var result = await _superAdminService.DeactivateAdminAsync(pk, ct);
            if (result.IsNotFound) return NotFound(WebApplication1.Models.Responses.ApiResponse<object>.Fail(result.Message));
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(new { }, "Admin deactivated"));
        }

        [WebApplication1.Utils.AuthorizePermission(WebApplication1.Utils.Permissions.AdminManagement.View)]
        [HttpGet("permissions")]
        public async Task<IActionResult> PermissionsListAsync(System.Threading.CancellationToken ct)
        {
            var result = await _superAdminService.GetPermissionsListAsync(ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [WebApplication1.Utils.AuthorizePermission(WebApplication1.Utils.Permissions.AdminManagement.ChangePermissions)]
        [HttpPost("permissions/assign")]
        public async Task<IActionResult> PermissionsAssignAsync([FromBody] PermissionPayload payload, System.Threading.CancellationToken ct)
        {
            var result = await _superAdminService.AssignPermissionsAsync(payload, ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [WebApplication1.Utils.AuthorizePermission(WebApplication1.Utils.Permissions.Backup.Create)]
        [HttpPost("backup/create")]
        public async Task<IActionResult> BackupCreateAsync(System.Threading.CancellationToken ct)
        {
            var result = await _superAdminService.CreateBackupAsync(ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data, "Backup created"));
        }

        [WebApplication1.Utils.AuthorizePermission(WebApplication1.Utils.Permissions.Backup.Download)]
        [HttpGet("backup/list")]
        public async Task<IActionResult> BackupListAsync(System.Threading.CancellationToken ct)
        {
            var result = await _superAdminService.GetBackupListAsync(ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        public record RestoreBackupRequest(string BackupId);

        [WebApplication1.Utils.AuthorizePermission(WebApplication1.Utils.Permissions.Backup.Restore)]
        [HttpPost("backup/restore")]
        public async Task<IActionResult> BackupRestoreAsync([FromBody] RestoreBackupRequest request, System.Threading.CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request?.BackupId))
                return BadRequest(WebApplication1.Models.Responses.ApiResponse<object>.Fail("backup_id is required"));
            var result = await _superAdminService.RestoreBackupAsync(request.BackupId, ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data, "Backup restored"));
        }
        [WebApplication1.Utils.AuthorizePermission(WebApplication1.Utils.Permissions.Backup.Download)]
        [HttpGet("backup/{id}/download")]
        [ProducesResponseType(typeof(WebApplication1.Models.Responses.ApiResponse<object>), 200)]
        public async Task<IActionResult> BackupDownload(string id, System.Threading.CancellationToken ct)
        {
            var result = await _superAdminService.GetBackupDataAsync(id, ct);
            if (result.IsNotFound) return NotFound(WebApplication1.Models.Responses.ApiResponse<object>.Fail("Backup file not found."));

            if (result.Data is not byte[] bytes)
                return StatusCode(500, WebApplication1.Models.Responses.ApiResponse<object>.Fail("Backup data is corrupted."));

            return File(bytes, "application/json", $"{id}.json");
        }

        [WebApplication1.Utils.AuthorizePermission(WebApplication1.Utils.Permissions.AuditLogs.View)]
        [HttpGet("activity-log")]
        public async Task<IActionResult> ActivityLogAsync([FromQuery] int page = 1, [FromQuery] int page_size = 10, System.Threading.CancellationToken ct = default)
        {
            page_size = System.Math.Clamp(page_size, 1, 100);
            page = System.Math.Max(1, page);
            var result = await _superAdminService.GetActivityLogAsync(page, page_size, ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        // Health check requires super_admin — use /api/v1/superadmin/health with a valid super_admin token
        [HttpGet("health")]
        public async Task<IActionResult> HealthAsync(System.Threading.CancellationToken ct)
        {
            var startTime = System.Diagnostics.Stopwatch.GetTimestamp();
            bool dbHealthy = false;
            try
            {
                var scope = HttpContext.RequestServices.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<WebApplication1.Data.ApplicationDbContext>();
                dbHealthy = await db.Database.CanConnectAsync(ct);
            }
            catch { /* DB connection failed */ }
            var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(startTime);

            var status = dbHealthy ? "healthy" : "degraded";
            var response = new
            {
                status,
                timestamp = System.DateTime.UtcNow.ToString("O"),
                checks = new
                {
                    database = dbHealthy ? "connected" : "unreachable",
                    response_time_ms = elapsed.TotalMilliseconds
                }
            };
            if (!dbHealthy)
                return StatusCode(503, WebApplication1.Models.Responses.ApiResponse<object>.Fail("Service degraded", response));
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(response));
        }
    }
}
