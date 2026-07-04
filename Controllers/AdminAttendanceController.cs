using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.Authorization;
using WebApplication1.Services;
using System.Collections.Generic;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/v1/admin")]
    [Authorize(Roles = "admin,super_admin")]
    public class AdminAttendanceController : ControllerBase
    {
        private readonly IAdminAttendanceService _adminAttendanceService;

        public AdminAttendanceController(IAdminAttendanceService adminAttendanceService)
        {
            _adminAttendanceService = adminAttendanceService;
        }

        [HttpGet("qr/current")]
        public async Task<IActionResult> QrCurrentAsync(CancellationToken ct)
        {
            var result = await _adminAttendanceService.GetCurrentQrAsync(ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("qr/history")]
        public async Task<IActionResult> QrHistoryAsync(CancellationToken ct, [FromQuery] int page = 1, [FromQuery] int page_size = 20)
        {
            var result = await _adminAttendanceService.GetQrHistoryAsync(page, page_size, ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        public class QrGenerateDto
        {
            [System.Text.Json.Serialization.JsonPropertyName("expiry_duration")]
            public string? ExpiryDuration { get; set; } // "1day", "7day", "1month"
        }

        [HttpPost("qr/generate")]
        public async Task<IActionResult> QrGenerateAsync([FromBody] QrGenerateDto? dto, CancellationToken ct)
        {
            var result = await _adminAttendanceService.GenerateQrAsync(dto?.ExpiryDuration, ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpPost("qr/regenerate")]
        public async Task<IActionResult> QrRegenerateAsync([FromBody] QrGenerateDto? dto, CancellationToken ct)
        {
            var result = await _adminAttendanceService.RegenerateQrAsync(dto?.ExpiryDuration, ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpPost("qr/expire")]
        public async Task<IActionResult> QrExpireAsync(CancellationToken ct)
        {
            await _adminAttendanceService.ExpireAllQrAsync(ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(new { }));
        }

        [HttpDelete("qr/{pk}")]
        public async Task<IActionResult> QrDeleteAsync(int pk, CancellationToken ct)
        {
            var result = await _adminAttendanceService.DeleteQrAsync(pk, ct);
            if (result.IsNotFound) return NotFound(new { success = false, message = result.Message });
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(new { }));
        }

        [Authorize(Roles = "super_admin")]
        [HttpDelete("qr/clear-all")]
        public async Task<IActionResult> QrClearAllAsync(CancellationToken ct)
        {
            await _adminAttendanceService.ClearAllQrAsync(ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(new { }));
        }

        [HttpGet("qr/{pk}/scans")]
        public async Task<IActionResult> QrScansAsync(int pk, CancellationToken ct)
        {
            var result = await _adminAttendanceService.GetQrScansAsync(pk, ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("holidays")]
        public async Task<IActionResult> HolidaysListAsync(CancellationToken ct, [FromQuery] string? from_date = null, [FromQuery] string? to_date = null, [FromQuery] string? date = null, [FromQuery] bool? is_active = null)
        {
            var result = await _adminAttendanceService.GetHolidaysAsync(from_date, to_date, date, is_active, ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }
        
        [HttpGet("holidays/{pk}")]
        public async Task<IActionResult> HolidayDetailAsync(int pk, CancellationToken ct)
        {
            var result = await _adminAttendanceService.GetHolidayDetailAsync(pk, ct);
            if (result.IsNotFound) return NotFound(new { success = false, message = result.Message });
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpPost("holidays")]
        public async Task<IActionResult> CreateHolidayAsync([FromBody] WebApplication1.Models.DTOs.Attendance.HolidayDto dto, CancellationToken ct)
        {
            var result = await _adminAttendanceService.CreateHolidayAsync(dto, ct);
            if (!result.Success) return BadRequest(new { success = false, message = result.Message });
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpPut("holidays/{pk}")]
        public async Task<IActionResult> UpdateHolidayAsync(int pk, [FromBody] WebApplication1.Models.DTOs.Attendance.HolidayDto dto, CancellationToken ct)
        {
            var result = await _adminAttendanceService.UpdateHolidayAsync(pk, dto, ct);
            if (result.IsNotFound) return NotFound(new { success = false, message = result.Message });
            if (!result.Success) return BadRequest(new { success = false, message = result.Message });
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpDelete("holidays/{pk}")]
        public async Task<IActionResult> DeleteHolidayAsync(int pk, CancellationToken ct)
        {
            var result = await _adminAttendanceService.DeleteHolidayAsync(pk, ct);
            if (result.IsNotFound) return NotFound(new { success = false, message = result.Message });
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(new { }));
        }

        [HttpGet("attendance/daily-summary")]
        public async Task<IActionResult> AttendanceDailySummaryAsync(CancellationToken ct, [FromQuery] string? date = null)
        {
            var result = await _adminAttendanceService.GetAttendanceDailySummaryAsync(date, ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("attendance/absentees")]
        public async Task<IActionResult> AttendanceAbsenteesAsync(CancellationToken ct, [FromQuery] string? date = null)
        {
            var result = await _adminAttendanceService.GetAttendanceAbsenteesAsync(date, ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("attendance/streak")]
        public async Task<IActionResult> AttendanceStreakAsync(CancellationToken ct)
        {
            var result = await _adminAttendanceService.GetAttendanceStreakAsync(ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpPost("attendance/manual")]
        public async Task<IActionResult> AttendanceManualAsync([FromBody] WebApplication1.Models.DTOs.Attendance.ManualAttendanceDto dto, CancellationToken ct)
        {
            var result = await _adminAttendanceService.RecordManualAttendanceAsync(dto, ct);
            if (!result.Success) return BadRequest(WebApplication1.Models.Responses.ApiResponse<object>.Fail(result.Message));
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(new { }, "Attendance manually recorded"));
        }

        [HttpPost("attendance/manual/bulk")]
        public async Task<IActionResult> AttendanceManualBulkAsync([FromBody] List<WebApplication1.Models.DTOs.Attendance.ManualAttendanceDto> dtos, CancellationToken ct)
        {
            var result = await _adminAttendanceService.RecordManualBulkAttendanceAsync(dtos, ct);
            if (!result.Success) return BadRequest(WebApplication1.Models.Responses.ApiResponse<object>.Fail(result.Message));
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data, result.Message));
        }

        [HttpGet("attendance")]
        public async Task<IActionResult> AttendanceListAsync(CancellationToken ct, [FromQuery] string? date = null, [FromQuery] string? from_date = null, [FromQuery] string? to_date = null, [FromQuery] int page = 1, [FromQuery] int page_size = 100)
        {
            page_size = Math.Clamp(page_size, 1, 500);
            page = Math.Max(1, page);
            var nextTemplate = $"{Request.Scheme}://{Request.Host}{Request.Path}?page={{P}}&page_size={page_size}{(date != null ? $"&date={date}" : "")}";
            var prevTemplate = $"{Request.Scheme}://{Request.Host}{Request.Path}?page={{P}}&page_size={page_size}{(date != null ? $"&date={date}" : "")}";

            var result = await _adminAttendanceService.GetAttendanceListAsync(date, from_date, to_date, page, page_size, nextTemplate, prevTemplate, ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("attendance/{pk}")]
        public async Task<IActionResult> AttendanceDetailAsync(int pk, CancellationToken ct)
        {
            var result = await _adminAttendanceService.GetAttendanceDetailAsync(pk, ct);
            if (result.IsNotFound) return NotFound(new { success = false, message = result.Message });
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }
    }
}
