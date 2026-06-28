using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Threading;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using WebApplication1.Services;
using WebApplication1.Models.Responses;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/v1/seats")]
    [Authorize]
    public class SeatsController : ControllerBase
    {
        private readonly IStudentSeatService _seatService;

        public SeatsController(IStudentSeatService seatService)
        {
            _seatService = seatService;
        }

        private long? GetCurrentUserId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("user_id");
            if (long.TryParse(userIdStr, out var userId)) return userId;
            return null;
        }

        [HttpGet("layout")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> GetSeatLayoutAsync(CancellationToken ct)
        {
            var result = await _seatService.GetSeatLayoutAsync(ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("history")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> GetSeatHistoryAsync(CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized(ApiResponse<object>.Fail("User not found"));

            var result = await _seatService.GetSeatHistoryAsync(userId.Value, ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }
    }
}
