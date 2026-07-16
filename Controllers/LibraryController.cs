using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Threading;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using WebApplication1.Services;
using WebApplication1.Models.Responses;
using System;
using WebApplication1.Models.DTOs.Library;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/v1")]
    public class LibraryController : BaseApiController
    {
        private readonly ILibraryService _libraryService;

        public LibraryController(ILibraryService libraryService, ICurrentUserService currentUserService) : base(currentUserService)
        {
            _libraryService = libraryService;
        }

        [AllowAnonymous]
        [HttpGet("library/info")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ResponseCache(Duration = 900)]
        public async Task<IActionResult> GetLibraryInfoAsync(CancellationToken ct)
        {
            var mediaBaseUrl = $"{Request.Scheme}://{Request.Host}";
            var result = await _libraryService.GetLibraryInfoAsync(mediaBaseUrl, ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [AllowAnonymous]
        [HttpGet("/favicon.ico")]
        [ProducesResponseType(typeof(WebApplication1.Models.Responses.ApiResponse<object>), 200)]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> GetFaviconAsync(CancellationToken ct)
        {
            var mediaBaseUrl = $"{Request.Scheme}://{Request.Host}";
            var result = await _libraryService.GetLibraryInfoAsync(mediaBaseUrl, ct);
            if (result.Success && result.Data != null)
            {
                var logoUrl = result.Data.GetType().GetProperty("logo")?.GetValue(result.Data) as string;
                if (!string.IsNullOrEmpty(logoUrl))
                {
                    return Redirect(logoUrl);
                }
            }
            return NotFound();
        }

        [AllowAnonymous]
        [HttpGet("library/facilities")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ResponseCache(Duration = 900)]
        public async Task<IActionResult> GetFacilitiesAsync(CancellationToken ct)
        {
            var mediaBaseUrl = $"{Request.Scheme}://{Request.Host}";
            var result = await _libraryService.GetFacilitiesAsync(mediaBaseUrl, ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [AllowAnonymous]
        [HttpGet("library/achievers")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ResponseCache(Duration = 900)]
        public async Task<IActionResult> GetAchieversAsync([FromQuery] bool? featured, CancellationToken ct)
        {
            var mediaBaseUrl = $"{Request.Scheme}://{Request.Host}";
            var result = await _libraryService.GetAchieversAsync(featured, mediaBaseUrl, ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [AllowAnonymous]
        [HttpGet("library/reviews")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> GetReviewsAsync(CancellationToken ct)
        {
            var result = await _libraryService.GetReviewsAsync(ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [AllowAnonymous]
        [HttpGet("library/reviews/summary")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ResponseCache(Duration = 900)]
        public async Task<IActionResult> GetReviewsSummaryAsync(CancellationToken ct)
        {
            var result = await _libraryService.GetReviewsSummaryAsync(ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("library/reviews/my")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> GetMyReviewAsync(CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(ApiResponse<object>.Fail("User not found"));

            var result = await _libraryService.GetMyReviewAsync(userId.Value, ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
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

        [AllowAnonymous]
        [HttpGet("sliders")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ResponseCache(Duration = 900)]
        public async Task<IActionResult> GetSlidersAsync(CancellationToken ct)
        {
            var mediaBaseUrl = $"{Request.Scheme}://{Request.Host}";
            var result = await _libraryService.GetSlidersAsync(mediaBaseUrl, ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [AllowAnonymous]
        [HttpGet("library/gallery")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ResponseCache(Duration = 900)]
        public async Task<IActionResult> GetGalleryImagesAsync(CancellationToken ct)
        {
            var mediaBaseUrl = $"{Request.Scheme}://{Request.Host}";
            var result = await _libraryService.GetGalleryImagesAsync(mediaBaseUrl, ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }
    }
}
