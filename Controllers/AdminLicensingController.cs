using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using WebApplication1.Services;
using WebApplication1.Models.Responses;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/v1/licensing")]
    [Authorize] // Handled by standard JWT middleware
    public class AdminLicensingController : ControllerBase
    {
        private readonly IAdminLicensingService _licensingService;

        public AdminLicensingController(IAdminLicensingService licensingService)
        {
            _licensingService = licensingService;
        }

        // ==========================
        // SUPER ADMIN ENDPOINTS
        // ==========================

        [HttpGet("platform-plans")]
        [Authorize(Roles = "super_admin,sub_super_admin")]
        public async Task<IActionResult> GetPlatformPlans(CancellationToken ct)
        {
            var result = await _licensingService.GetPlatformPlansAsync(ct);
            return result.Success ? Ok(ApiResponse<object>.Ok(result.Data)) : BadRequest(ApiResponse<object>.Fail(result.Message!));
        }

        [HttpPost("platform-plans")]
        [Authorize(Roles = "super_admin,sub_super_admin")]
        public async Task<IActionResult> CreatePlatformPlan([FromBody] PlatformPlanPayload payload, CancellationToken ct)
        {
            var result = await _licensingService.CreatePlatformPlanAsync(payload, ct);
            return result.Success ? Ok(ApiResponse<object>.Ok(result.Data)) : BadRequest(ApiResponse<object>.Fail(result.Message!));
        }

        [HttpGet("payment-settings")]
        [Authorize(Roles = "super_admin,sub_super_admin")]
        public async Task<IActionResult> GetPaymentSettings(CancellationToken ct)
        {
            var result = await _licensingService.GetPlatformPaymentSettingsAsync(ct);
            return result.Success ? Ok(ApiResponse<object>.Ok(result.Data)) : BadRequest(ApiResponse<object>.Fail(result.Message!));
        }

        [HttpPut("payment-settings")]
        [Authorize(Roles = "super_admin,sub_super_admin")]
        public async Task<IActionResult> UpdatePaymentSettings([FromBody] PlatformPaymentSettingsPayload payload, CancellationToken ct)
        {
            var result = await _licensingService.UpdatePlatformPaymentSettingsAsync(payload, ct);
            return result.Success ? Ok(ApiResponse<object>.Ok(result.Data)) : BadRequest(ApiResponse<object>.Fail(result.Message!));
        }

        [HttpGet("library-payments")]
        [Authorize(Roles = "super_admin,sub_super_admin")]
        public async Task<IActionResult> GetLibraryPayments(CancellationToken ct)
        {
            var result = await _licensingService.GetLibraryPaymentsAsync(ct);
            return result.Success ? Ok(ApiResponse<object>.Ok(result.Data)) : BadRequest(ApiResponse<object>.Fail(result.Message!));
        }

        [HttpPost("library-payments/{id}/approve")]
        [Authorize(Roles = "super_admin"), Authorize(Roles = "sub_super_admin"),]
        public async Task<IActionResult> ApproveLibraryPayment(long id, CancellationToken ct)
        {
            var userIdStr = User.FindFirstValue("user_id");
            long.TryParse(userIdStr, out var adminId);

            var result = await _licensingService.ApproveLibraryPaymentAsync(id, adminId, ct);
            return result.Success ? Ok(ApiResponse<object>.Ok(result.Data)) : BadRequest(ApiResponse<object>.Fail(result.Message!));
        }

        // ==========================
        // SUB SUPER ADMIN ENDPOINTS
        // ==========================

        [HttpGet("current-subscription")]
        // Accessible by sub_super_admin (and super_admin for viewing)
        public async Task<IActionResult> GetCurrentSubscription(CancellationToken ct)
        {
            var result = await _licensingService.GetCurrentSubscriptionAsync(ct);
            return result.Success ? Ok(ApiResponse<object>.Ok(result.Data)) : BadRequest(ApiResponse<object>.Fail(result.Message!));
        }

        [HttpPost("submit-payment")]
        public async Task<IActionResult> SubmitLibraryPayment([FromBody] LibraryPaymentSubmitPayload payload, CancellationToken ct)
        {
            var result = await _licensingService.SubmitLibraryPaymentAsync(payload, ct);
            return result.Success ? Ok(ApiResponse<object>.Ok(result.Data)) : BadRequest(ApiResponse<object>.Fail(result.Message!));
        }
        
        [HttpGet("public-payment-settings")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicPaymentSettings(CancellationToken ct)
        {
            var result = await _licensingService.GetPlatformPaymentSettingsAsync(ct);
            return result.Success ? Ok(ApiResponse<object>.Ok(result.Data)) : BadRequest(ApiResponse<object>.Fail(result.Message!));
        }
    }
}
