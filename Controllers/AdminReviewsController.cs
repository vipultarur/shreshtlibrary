using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
using WebApplication1.Models.Responses;
using WebApplication1.Services;
using WebApplication1.Models.DTOs.Admin;

namespace WebApplication1.Controllers
{
    [Route("api/v1/admin/reviews")]
    [ApiController]
    [Authorize(Roles = WebApplication1.Utils.Constants.Roles.SuperAdmin + "," + WebApplication1.Utils.Constants.Roles.Admin)]
    public class AdminReviewsController : ControllerBase
    {
        private readonly IAdminLibraryService _libraryService;

        public AdminReviewsController(IAdminLibraryService libraryService)
        {
            _libraryService = libraryService;
        }

        [HttpGet("")]
        public async Task<IActionResult> GetReviews([FromQuery] int page = 1, [FromQuery] int page_size = 20, CancellationToken ct = default)
        {
            var result = await _libraryService.GetReviews(page, page_size, ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingReviews([FromQuery] int page = 1, [FromQuery] int page_size = 20, CancellationToken ct = default)
        {
            var result = await _libraryService.GetPendingReviews(page, page_size, ct);
            return Ok(ApiResponse<object>.Ok(result.Data)); 
        }
        [HttpPost("{id}/approve")]
        public async Task<IActionResult> ApproveReview(long id, CancellationToken ct)
        {
            var result = await _libraryService.ApproveReview(id, ct);
            if (result.IsNotFound) return NotFound(ApiResponse<object>.Fail(result.Message));
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpPost("{id}/reject")]
        public async Task<IActionResult> RejectReview(long id, [FromBody] RejectReviewDto dto, CancellationToken ct)
        {
            var result = await _libraryService.RejectReview(id, dto.Reason, ct);
            if (result.IsNotFound) return NotFound(ApiResponse<object>.Fail(result.Message));
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpDelete("{id}/delete")]
        public async Task<IActionResult> DeleteReview(long id, CancellationToken ct)
        {
            var result = await _libraryService.DeleteReview(id, ct);
            if (result.IsNotFound) return NotFound(ApiResponse<object>.Fail(result.Message));
            return Ok(ApiResponse<object>.Ok(result.Data));
        }
    }
}
