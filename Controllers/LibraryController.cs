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
    [Route("api/v1")]
    public class LibraryController : ControllerBase
    {
        private readonly ILibraryService _libraryService;

        public LibraryController(ILibraryService libraryService)
        {
            _libraryService = libraryService;
        }

        private long? GetCurrentUserId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("user_id");
            if (long.TryParse(userIdStr, out var userId)) return userId;
            return null;
        }

        [HttpGet("library/info")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> GetLibraryInfoAsync(CancellationToken ct)
        {
            var result = await _libraryService.GetLibraryInfoAsync(ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("library/facilities")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> GetFacilitiesAsync(CancellationToken ct)
        {
            var result = await _libraryService.GetFacilitiesAsync(ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("library/achievers")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> GetAchieversAsync([FromQuery] bool? featured, CancellationToken ct)
        {
            var mediaBaseUrl = $"{Request.Scheme}://{Request.Host}";
            var result = await _libraryService.GetAchieversAsync(featured, mediaBaseUrl, ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("library/reviews")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> GetReviewsAsync(CancellationToken ct)
        {
            var result = await _libraryService.GetReviewsAsync(ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("library/reviews/summary")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> GetReviewsSummaryAsync(CancellationToken ct)
        {
            var result = await _libraryService.GetReviewsSummaryAsync(ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        public class SubmitReviewRequest
        {
            public int rating { get; set; }
            public string comment { get; set; } = string.Empty;
        }

        [HttpPost("library/reviews/submit")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> SubmitReviewAsync([FromBody] SubmitReviewRequest request, CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(ApiResponse<object>.Fail("User not found"));

            var result = await _libraryService.SubmitReviewAsync(userId.Value, request, ct);
            if (!result.Success) return BadRequest(ApiResponse<object>.Fail(result.Message!));
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("sliders")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> GetSlidersAsync(CancellationToken ct)
        {
            var mediaBaseUrl = $"{Request.Scheme}://{Request.Host}";
            var result = await _libraryService.GetSlidersAsync(mediaBaseUrl, ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }
    }
}
