using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using WebApplication1.Models.Responses;
using WebApplication1.Services;
using System;
using WebApplication1.Models.DTOs.Attendance;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/v1")]
    [Authorize]
    public class AttendanceController : BaseApiController
    {
        private readonly IAttendanceService _attendanceService;

        public AttendanceController(IAttendanceService attendanceService, ICurrentUserService currentUserService) : base(currentUserService)
        {
            _attendanceService = attendanceService;
        }

        [HttpGet("qr/today")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> GetTodayQrAsync(CancellationToken ct)
        {
            var qr = await _attendanceService.GetTodayQrAsync(ct);
            return Ok(ApiResponse<object>.Ok(qr));
        }

        [HttpPost("attendance/scan")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> ScanQrAsync([FromBody] ScanQrRequest request, CancellationToken ct)
        {
            if (!TryGetUserId(out var userId)) return UnauthorizedResponse("User not found");

            if (string.IsNullOrEmpty(request?.qr_hash))
            {
                return BadRequest(ApiResponse<object>.Fail("QR hash is required"));
            }

            try
            {
                var result = await _attendanceService.ScanQrAsync(userId, request.qr_hash, ct);
                return Ok(ApiResponse<object>.Ok(result));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

        [HttpPost("attendance/checkout")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> CheckoutAsync(CancellationToken ct)
        {
            if (!TryGetUserId(out var userId)) return UnauthorizedResponse("User not found");

            try
            {
                var result = await _attendanceService.CheckoutAsync(userId, ct);
                return Ok(ApiResponse<object>.Ok(result));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

        [HttpGet("attendance/logs")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> GetAttendanceLogsAsync(CancellationToken ct)
        {
            if (!TryGetUserId(out var userId)) return UnauthorizedResponse("User not found");

            var logs = await _attendanceService.GetAttendanceLogsAsync(userId, ct);
            return Ok(ApiResponse<object>.Ok(logs));
        }

        [AllowAnonymous]
        [HttpGet("holidays")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> GetHolidaysAsync([FromQuery] string? from_date = null, [FromQuery] string? to_date = null, [FromQuery] string? date = null, [FromQuery] bool? is_active = null, CancellationToken ct = default)
        {
            var holidays = await _attendanceService.GetHolidaysAsync(from_date, to_date, date, is_active, ct);
            return Ok(ApiResponse<object>.Ok(holidays));
        }
    }
}
