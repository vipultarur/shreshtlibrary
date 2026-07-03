using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/v1/superadmin")]
    [Authorize(Roles = "super_admin")]
    public class SuperAdminController : ControllerBase
    {
        public class AdminPayload
        {
            public string? Username { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("first_name")]
            public string? FirstName { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("last_name")]
            public string? LastName { get; set; }
            public string? Email { get; set; }
            public string? Mobile { get; set; }
            public string? Password { get; set; }
            public string? Role { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("is_active")]
            public bool? IsActive { get; set; }
            public System.Collections.Generic.Dictionary<string, bool>? Permissions { get; set; }
        }

        public class PermissionPayload
        {
            public long AdminId { get; set; }
            public string[]? Permissions { get; set; }
        }

        private readonly WebApplication1.Services.ISuperAdminService _superAdminService;

        public SuperAdminController(WebApplication1.Services.ISuperAdminService superAdminService)
        {
            _superAdminService = superAdminService;
        }

        [HttpPost("admins")]
        public async Task<IActionResult> AdminsAddAsync([FromBody] AdminPayload payload, System.Threading.CancellationToken ct)
        {
            var result = await _superAdminService.AddAdminAsync(payload, ct);
            if (!result.Success) return BadRequest(WebApplication1.Models.Responses.ApiResponse<object>.Fail(result.Message, result.Errors));
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpPut("admins/{pk}")]
        public async Task<IActionResult> AdminsUpdateAsync(long pk, [FromBody] AdminPayload payload, System.Threading.CancellationToken ct)
        {
            var result = await _superAdminService.UpdateAdminAsync(pk, payload, ct);
            if (!result.Success) return BadRequest(WebApplication1.Models.Responses.ApiResponse<object>.Fail(result.Message, result.Errors));
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("admins")]
        public async Task<IActionResult> AdminsListAsync(System.Threading.CancellationToken ct)
        {
            var result = await _superAdminService.GetAdminsListAsync(ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("admins/{pk}")]
        public async Task<IActionResult> AdminDetailAsync(long pk, System.Threading.CancellationToken ct)
        {
            var result = await _superAdminService.GetAdminDetailAsync(pk, ct);
            if (result.IsNotFound) return NotFound(WebApplication1.Models.Responses.ApiResponse<object>.Fail(result.Message));
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpDelete("admins/{pk}/remove")]
        public async Task<IActionResult> AdminRemoveAsync(long pk, System.Threading.CancellationToken ct)
        {
            var result = await _superAdminService.RemoveAdminAsync(pk, ct);
            if (result.IsNotFound) return NotFound(WebApplication1.Models.Responses.ApiResponse<object>.Fail(result.Message));
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(new { }, "Admin removed"));
        }

        [HttpPost("admins/{pk}/deactivate")]
        public async Task<IActionResult> AdminDeactivateAsync(long pk, System.Threading.CancellationToken ct)
        {
            var result = await _superAdminService.DeactivateAdminAsync(pk, ct);
            if (result.IsNotFound) return NotFound(WebApplication1.Models.Responses.ApiResponse<object>.Fail(result.Message));
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(new { }, "Admin deactivated"));
        }

        [HttpGet("permissions")]
        public async Task<IActionResult> PermissionsListAsync(System.Threading.CancellationToken ct)
        {
            var result = await _superAdminService.GetPermissionsListAsync(ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpPost("permissions/assign")]
        public async Task<IActionResult> PermissionsAssignAsync([FromBody] PermissionPayload payload, System.Threading.CancellationToken ct)
        {
            var result = await _superAdminService.AssignPermissionsAsync(payload, ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpPost("backup/create")]
        public async Task<IActionResult> BackupCreateAsync(System.Threading.CancellationToken ct)
        {
            var result = await _superAdminService.CreateBackupAsync(ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data, "Backup created"));
        }

        [HttpGet("backup/list")]
        public async Task<IActionResult> BackupListAsync(System.Threading.CancellationToken ct)
        {
            var result = await _superAdminService.GetBackupListAsync(ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpPost("backup/restore")]
        public async Task<IActionResult> BackupRestoreAsync([FromQuery] string backup_id, System.Threading.CancellationToken ct)
        {
            var result = await _superAdminService.RestoreBackupAsync(backup_id, ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data, "Backup restored"));
        }
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

        [HttpGet("activity-log")]
        public async Task<IActionResult> ActivityLogAsync([FromQuery] int page = 1, [FromQuery] int page_size = 10, System.Threading.CancellationToken ct = default)
        {
            page_size = System.Math.Clamp(page_size, 1, 100);
            page = System.Math.Max(1, page);
            var result = await _superAdminService.GetActivityLogAsync(page, page_size, ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("health")]
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
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
