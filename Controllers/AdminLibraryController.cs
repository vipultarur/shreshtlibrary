using Microsoft.AspNetCore.Mvc;
using WebApplication1.Services;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.Http;
using WebApplication1.Models.Responses;

namespace WebApplication1.Controllers
{
    [Route("api/v1/admin/library")]
    [ApiController]
    [Authorize(Roles = "admin,super_admin")]
    public class AdminLibraryController : ControllerBase
    {
        private readonly IAdminLibraryService _libraryService;

        public AdminLibraryController(IAdminLibraryService libraryService)
        {
            _libraryService = libraryService;
        }

        [HttpGet("info")]
        public async Task<IActionResult> GetLibraryInfo(CancellationToken ct)
        {
            var result = await _libraryService.GetLibraryInfo(ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpPost("info")]
        public async Task<IActionResult> UpdateLibraryInfo([FromForm] LibraryInfoUpdateDto dto, CancellationToken ct)
        {
            var result = await _libraryService.UpdateLibraryInfo(dto, ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("facilities")]
        public async Task<IActionResult> GetFacilities(CancellationToken ct)
        {
            var result = await _libraryService.GetFacilities(ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpPost("facilities")]
        public async Task<IActionResult> CreateFacility([FromForm] FacilityDto dto, CancellationToken ct)
        {
            var result = await _libraryService.CreateFacility(dto, ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpPut("facilities/{id}")]
        public async Task<IActionResult> UpdateFacility(long id, [FromForm] FacilityDto dto, CancellationToken ct)
        {
            var result = await _libraryService.UpdateFacility(id, dto, ct);
            if (result.IsNotFound) return NotFound(ApiResponse<object>.Fail(result.Message));
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpPost("facilities/{id}/toggle")]
        public async Task<IActionResult> ToggleFacility(long id, CancellationToken ct)
        {
            var result = await _libraryService.ToggleFacility(id, ct);
            if (result.IsNotFound) return NotFound(ApiResponse<object>.Fail(result.Message));
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpDelete("facilities/{id}")]
        public async Task<IActionResult> DeleteFacility(long id, CancellationToken ct)
        {
            var result = await _libraryService.DeleteFacility(id, ct);
            if (result.IsNotFound) return NotFound(ApiResponse<object>.Fail(result.Message));
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("achievers")]
        public async Task<IActionResult> GetAchievers(CancellationToken ct)
        {
            var result = await _libraryService.GetAchievers(ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpPost("achievers")]
        public async Task<IActionResult> CreateAchiever([FromForm] AchieverDto dto, CancellationToken ct)
        {
            var result = await _libraryService.CreateAchiever(dto, ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpPut("achievers/{id}")]
        public async Task<IActionResult> UpdateAchiever(long id, [FromForm] AchieverDto dto, CancellationToken ct)
        {
            var result = await _libraryService.UpdateAchiever(id, dto, ct);
            if (result.IsNotFound) return NotFound(ApiResponse<object>.Fail(result.Message));
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpPost("achievers/{id}/toggle")]
        public async Task<IActionResult> ToggleAchiever(long id, CancellationToken ct)
        {
            var result = await _libraryService.ToggleAchiever(id, ct);
            if (result.IsNotFound) return NotFound(ApiResponse<object>.Fail(result.Message));
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpDelete("achievers/{id}")]
        public async Task<IActionResult> DeleteAchiever(long id, CancellationToken ct)
        {
            var result = await _libraryService.DeleteAchiever(id, ct);
            if (result.IsNotFound) return NotFound(ApiResponse<object>.Fail(result.Message));
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("reviews")]
        public async Task<IActionResult> GetReviews(CancellationToken ct)
        {
            var result = await _libraryService.GetReviews(ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("reviews/summary")]
        public async Task<IActionResult> GetReviewSummary(CancellationToken ct)
        {
            var result = await _libraryService.GetReviewSummary(ct);
            return Ok(ApiResponse<object>.Ok(result.Data));
        }

        public class LibraryInfoUpdateDto
        {
            public string? Name { get; set; }
            public string? ContactEmail { get; set; }
            public string? ContactPhone { get; set; }
            public string? Address { get; set; }
            public string? TimingInfo { get; set; }
            public IFormFile? Logo { get; set; }
        }

        public class FacilityDto
        {
            public string Name { get; set; }
            public string? Description { get; set; }
            public IFormFile? Image { get; set; }
            public int? Order { get; set; }
            public bool? IsActive { get; set; }
        }

        public class AchieverDto
        {
            public string Name { get; set; }
            public string? Achievement { get; set; }
            public string? ExamName { get; set; }
            public IFormFile? Photo { get; set; }
            public int? Order { get; set; }
            public bool? IsActive { get; set; }
        }
    }
}
