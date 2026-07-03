using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.Authorization;
using WebApplication1.Models.Responses;
using WebApplication1.Services;
using WebApplication1.Models.DTOs.Student;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/v1/student")]
    [Authorize]
    public class StudentController : ControllerBase
    {
        private readonly IStudentProfileService _profileService;
        private readonly IStudentDashboardService _dashboardService;
        private readonly IStudentReferralService _referralService;
        private readonly ICurrentUserService _currentUserService;

        public StudentController(
            IStudentProfileService profileService,
            IStudentDashboardService dashboardService,
            IStudentReferralService referralService,
            ICurrentUserService currentUserService)
        {
            _profileService = profileService;
            _dashboardService = dashboardService;
            _referralService = referralService;
            _currentUserService = currentUserService;
        }

        private long? GetCurrentUserId()
        {
            return _currentUserService.GetUserId();
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfileAsync(CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(ApiResponse<object>.Fail("Unauthorized"));

            var result = await _profileService.GetProfileAsync(userId.Value, Request.Scheme, Request.Host.ToString(), ct);
            if (result == null) return NotFound(ApiResponse<object>.Fail("Profile not found"));

            return Ok(result);
        }

        [HttpPut("profile/update")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> UpdateProfileAsync([FromBody] UpdateProfileDto dto, CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(ApiResponse<object>.Fail("Unauthorized"));

            var result = await _profileService.UpdateProfileAsync(userId.Value, dto, ct);
            if (result == null) return NotFound(ApiResponse<object>.Fail("Profile not found"));

            return Ok(result);
        }

        [HttpPost("profile/photo")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> UploadPhotoAsync(Microsoft.AspNetCore.Http.IFormFile profile_photo, CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(ApiResponse<object>.Fail("User not found"));

            var result = await _profileService.UploadPhotoAsync(userId.Value, profile_photo, Request.Scheme, Request.Host.ToString(), ct);
            if (!result.Success) return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardAsync(CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(ApiResponse<object>.Fail("Unauthorized"));

            var result = await _dashboardService.GetDashboardAsync(userId.Value, ct);
            if (result == null) return NotFound(ApiResponse<object>.Fail("Profile not found"));

            return Ok(result);
        }

        [HttpGet("id-card")]
        public async Task<IActionResult> GetIdCardAsync(CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(ApiResponse<object>.Fail("Unauthorized"));

            var result = await _profileService.GetIdCardAsync(userId.Value, Request.Scheme, Request.Host.ToString(), ct);
            if (result == null) return NotFound(ApiResponse<object>.Fail("Profile not found"));

            return Ok(result);
        }

        [HttpGet("referral")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> GetReferralCodeAsync(CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(ApiResponse<object>.Fail("User not found"));

            var result = await _referralService.GetReferralCodeAsync(userId.Value, ct);
            if (!result.Success) return NotFound(result);

            return Ok(result);
        }

        [HttpPost("referral")]
        [ProducesResponseType(typeof(ApiResponse<object>), 201)]
        public async Task<IActionResult> GenerateReferralCodeAsync(CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(ApiResponse<object>.Fail("User not found"));

            var result = await _referralService.GenerateReferralCodeAsync(userId.Value, ct);
            if (!result.Success) return BadRequest(result);

            return StatusCode(201, result);
        }

        public class ApplyReferralRequest
        {
            public string code { get; set; }
        }

        [HttpPost("referral/apply")]
        public async Task<IActionResult> ApplyReferralAsync([FromBody] ApplyReferralRequest request, CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(ApiResponse<object>.Fail("User not found"));

            var result = await _referralService.ApplyReferralAsync(userId.Value, request?.code, ct);
            if (!result.Success) return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("referral/history")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> GetReferralHistoryAsync(CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(ApiResponse<object>.Fail("User not found"));

            var result = await _referralService.GetReferralHistoryAsync(userId.Value, ct);
            return Ok(result);
        }
    }
}
