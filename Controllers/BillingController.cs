using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Threading;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using WebApplication1.Services;
using WebApplication1.Models.Responses;
using WebApplication1.Models.DTOs.Billing;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/v1")]
    [Authorize]
    public class BillingController : BaseApiController
    {
        private readonly IPaymentService _paymentService;
        private readonly IAdminBillingService _adminBillingService;
        private readonly WebApplication1.Data.ApplicationDbContext _context;

        public BillingController(
            IPaymentService paymentService, 
            IAdminBillingService adminBillingService, 
            WebApplication1.Data.ApplicationDbContext context, 
            ICurrentUserService currentUserService) : base(currentUserService)
        {
            _paymentService = paymentService;
            _adminBillingService = adminBillingService;
            _context = context;
        }

        [AllowAnonymous]
        [HttpGet("plans")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> GetPublicPlansAsync(CancellationToken ct)
        {
            var result = await _paymentService.GetPublicPlansAsync(ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("memberships/plans")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> GetMembershipPlansAsync(CancellationToken ct)
        {
            var result = await _paymentService.GetMembershipPlansAsync(ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("memberships/history")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> GetMembershipHistoryAsync(CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(ApiResponse<object>.Fail("Authentication required."));

            var result = await _paymentService.GetMembershipHistoryAsync(userId.Value, ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("UserRateThrottle")]
        [HttpPost("payments/initiate")]
        [ProducesResponseType(typeof(ApiResponse<object>), 201)]
        public async Task<IActionResult> InitiatePaymentAsync([FromBody] InitiatePaymentPayload payload, CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(ApiResponse<object>.Fail("Authentication required."));

            var result = await _paymentService.InitiatePaymentAsync(userId.Value, payload, ct);
            if (!result.Success)
            {
                return BadRequest(ApiResponse<object>.Fail(result.Message!));
            }

            return StatusCode(201, ApiResponse<object>.Ok(result.Data, result.Message));
        }

        [HttpGet("payments/history")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> GetPaymentHistoryAsync(CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(ApiResponse<object>.Fail("Authentication required."));

            var result = await _paymentService.GetPaymentHistoryAsync(userId.Value, ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("payments/{pk}/receipt")]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        public async Task<IActionResult> GetPaymentReceiptAsync(long pk, CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(ApiResponse<object>.Fail("Authentication required."));

            var payment = await _context.PaymentsPayments.FindAsync(new object[] { pk }, ct);
            if (payment == null || payment.StudentId != userId.Value)
            {
                return NotFound(ApiResponse<object>.Fail("Payment not found or access denied."));
            }

            var result = await _adminBillingService.GetPaymentReceiptPdfAsync(pk, ct);
            if (!result.Success) return NotFound(ApiResponse<object>.Fail(result.Message!));

            return File((byte[])result.Data!, "application/pdf", $"receipt-{pk}.pdf");
        }
    }
}
