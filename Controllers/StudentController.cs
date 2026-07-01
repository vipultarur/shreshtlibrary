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
        private readonly IStudentService _studentService;
        private readonly ICurrentUserService _currentUserService;

        public StudentController(IStudentService studentService, ICurrentUserService currentUserService)
        {
            _studentService = studentService;
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

            var result = await _studentService.GetProfileAsync(userId.Value, Request.Scheme, Request.Host.ToString(), ct);
            if (result == null) return NotFound(ApiResponse<object>.Fail("Profile not found"));

            return Ok(result);
        }

        [HttpPut("profile/update")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> UpdateProfileAsync([FromBody] UpdateProfileDto dto, CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(ApiResponse<object>.Fail("Unauthorized"));

            var result = await _studentService.UpdateProfileAsync(userId.Value, dto, ct);
            if (result == null) return NotFound(ApiResponse<object>.Fail("Profile not found"));

            return Ok(result);
        }

        [HttpPost("profile/photo")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> UploadPhotoAsync(Microsoft.AspNetCore.Http.IFormFile profile_photo, CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(ApiResponse<object>.Fail("User not found"));

            var result = await _studentService.UploadPhotoAsync(userId.Value, profile_photo, Request.Scheme, Request.Host.ToString(), ct);
            if (!result.Success) return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardAsync(CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(ApiResponse<object>.Fail("Unauthorized"));

            var result = await _studentService.GetDashboardAsync(userId.Value, ct);
            if (result == null) return NotFound(ApiResponse<object>.Fail("Profile not found"));

            return Ok(result);
        }

        [HttpGet("id-card")]
        public async Task<IActionResult> GetIdCardAsync(CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(ApiResponse<object>.Fail("Unauthorized"));

            var result = await _studentService.GetIdCardAsync(userId.Value, Request.Scheme, Request.Host.ToString(), ct);
            if (result == null) return NotFound(ApiResponse<object>.Fail("Profile not found"));

            return Ok(result);
        }

        [HttpGet("referral")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> GetReferralCodeAsync(CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(ApiResponse<object>.Fail("User not found"));

            var result = await _studentService.GetReferralCodeAsync(userId.Value, ct);
            if (!result.Success) return NotFound(result);

            return Ok(result);
        }

        [HttpPost("referral")]
        [ProducesResponseType(typeof(ApiResponse<object>), 201)]
        public async Task<IActionResult> GenerateReferralCodeAsync(CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(ApiResponse<object>.Fail("User not found"));

            var result = await _studentService.GenerateReferralCodeAsync(userId.Value, ct);
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

            var result = await _studentService.ApplyReferralAsync(userId.Value, request?.code, ct);
            if (!result.Success) return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("referral/history")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> GetReferralHistoryAsync(CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(ApiResponse<object>.Fail("User not found"));

            var result = await _studentService.GetReferralHistoryAsync(userId.Value, ct);
            return Ok(result);
        }
    }
}
