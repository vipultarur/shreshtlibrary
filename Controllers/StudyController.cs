using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Threading;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using WebApplication1.Services;
using WebApplication1.Models.Responses;
using System;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/v1/study")]
    [Authorize]
    public class StudyController : ControllerBase
    {
        private readonly IStudyService _studyService;
        private readonly ICurrentUserService _currentUserService;

        public StudyController(IStudyService studyService, ICurrentUserService currentUserService)
        {
            _studyService = studyService;
            _currentUserService = currentUserService;
        }

        private long? GetCurrentUserId()
        {
            return _currentUserService.GetUserId();
        }

        [HttpPost("session/start")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> StartSessionAsync(CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(ApiResponse<object>.Fail("User not found"));

            var result = await _studyService.StartSessionAsync(userId.Value, ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        public class EndSessionRequest
        {
            [System.ComponentModel.DataAnnotations.Range(0, 1440, ErrorMessage = "Duration minutes cannot exceed 1440 (24 hours).")]
            public int duration_minutes { get; set; }

            [System.ComponentModel.DataAnnotations.Range(0, 1440, ErrorMessage = "Paused minutes cannot exceed 1440.")]
            public int paused_minutes { get; set; }
        }

        [HttpPost("session/end")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> EndSessionAsync([FromBody] EndSessionRequest request, CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(ApiResponse<object>.Fail("User not found"));

            var result = await _studyService.EndSessionAsync(userId.Value, request, ct);
            if (!result.Success) return BadRequest(ApiResponse<object>.Fail(result.Message!));
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("session/current")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> GetCurrentSessionAsync(CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(ApiResponse<object>.Fail("User not found"));

            var result = await _studyService.GetCurrentSessionAsync(userId.Value, ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        public class UpdateSessionRequest
        {
            public string status { get; set; } = string.Empty;

            [System.ComponentModel.DataAnnotations.Range(0, 1440, ErrorMessage = "Duration minutes cannot exceed 1440 (24 hours).")]
            public int? duration_minutes { get; set; }

            [System.ComponentModel.DataAnnotations.Range(0, 1440, ErrorMessage = "Paused minutes cannot exceed 1440.")]
            public int? paused_minutes { get; set; }
        }

        [HttpPost("session/update")]
        [HttpPut("session/update")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> UpdateSessionAsync([FromBody] UpdateSessionRequest request, CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(ApiResponse<object>.Fail("User not found"));

            var result = await _studyService.UpdateSessionAsync(userId.Value, request, ct);
            if (!result.Success) return BadRequest(ApiResponse<object>.Fail(result.Message!));
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("session/history")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> GetSessionHistoryAsync([FromQuery] int page = 1, [FromQuery] int page_size = 20, CancellationToken ct = default)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(ApiResponse<object>.Fail("User not found"));

            page_size = Math.Clamp(page_size, 1, 100);
            page = Math.Max(1, page);

            var result = await _studyService.GetSessionHistoryAsync(userId.Value, page, page_size, ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("leaderboard")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> GetLeaderboardAsync([FromQuery] string duration = "month", [FromQuery] string? start_date = null, [FromQuery] string? end_date = null, CancellationToken ct = default)
        {
            var mediaBaseUrl = $"{Request.Scheme}://{Request.Host}";
            var result = await _studyService.GetLeaderboardAsync(duration, start_date, end_date, mediaBaseUrl, ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }
    }
}
