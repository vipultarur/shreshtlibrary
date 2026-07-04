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

        public BillingController(IPaymentService paymentService, ICurrentUserService currentUserService) : base(currentUserService)
        {
            _paymentService = paymentService;
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
    }
}
