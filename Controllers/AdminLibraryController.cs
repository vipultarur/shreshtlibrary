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
            [FromForm(Name = "library_name")] public string? LibraryName { get; set; }
            [FromForm(Name = "logo")] public IFormFile? Logo { get; set; }
            [FromForm(Name = "banner_image")] public IFormFile? BannerImage { get; set; }
            [FromForm(Name = "description")] public string? Description { get; set; }
            [FromForm(Name = "established_year")] public int? EstablishedYear { get; set; }
            [FromForm(Name = "owner_name")] public string? OwnerName { get; set; }
            [FromForm(Name = "contact_number")] public string? ContactNumber { get; set; }
            [FromForm(Name = "email")] public string? Email { get; set; }
            [FromForm(Name = "website")] public string? Website { get; set; }
            [FromForm(Name = "opening_time")] public string? OpeningTime { get; set; }
            [FromForm(Name = "closing_time")] public string? ClosingTime { get; set; }
            [FromForm(Name = "weekly_off")] public string? WeeklyOff { get; set; }
            [FromForm(Name = "total_capacity")] public int? TotalCapacity { get; set; }
            [FromForm(Name = "available_seats")] public int? AvailableSeats { get; set; }
            [FromForm(Name = "address_line1")] public string? AddressLine1 { get; set; }
            [FromForm(Name = "address_line2")] public string? AddressLine2 { get; set; }
            [FromForm(Name = "area")] public string? Area { get; set; }
            [FromForm(Name = "city")] public string? City { get; set; }
            [FromForm(Name = "state")] public string? State { get; set; }
            [FromForm(Name = "country")] public string? Country { get; set; }
            [FromForm(Name = "pin_code")] public string? PinCode { get; set; }
            [FromForm(Name = "latitude")] public decimal? Latitude { get; set; }
            [FromForm(Name = "longitude")] public decimal? Longitude { get; set; }
            [FromForm(Name = "google_map_url")] public string? GoogleMapUrl { get; set; }

            [FromForm(Name = "wifi")] public bool? Wifi { get; set; }
            [FromForm(Name = "ac")] public bool? Ac { get; set; }
            [FromForm(Name = "cctv")] public bool? Cctv { get; set; }
            [FromForm(Name = "drinking_water")] public bool? DrinkingWater { get; set; }
            [FromForm(Name = "lockers")] public bool? Lockers { get; set; }
            [FromForm(Name = "charging_points")] public bool? ChargingPoints { get; set; }
            [FromForm(Name = "parking")] public bool? Parking { get; set; }
            [FromForm(Name = "reading_area")] public bool? ReadingArea { get; set; }
            [FromForm(Name = "computer_access")] public bool? ComputerAccess { get; set; }
            [FromForm(Name = "printing")] public bool? Printing { get; set; }

            [FromForm(Name = "facebook_url")] public string? FacebookUrl { get; set; }
            [FromForm(Name = "instagram_url")] public string? InstagramUrl { get; set; }
            [FromForm(Name = "whatsapp_number")] public string? WhatsappNumber { get; set; }
            [FromForm(Name = "telegram_url")] public string? TelegramUrl { get; set; }
            [FromForm(Name = "youtube_url")] public string? YoutubeUrl { get; set; }
        }

        public class FacilityDto
        {
            [FromForm(Name = "name")] public string Name { get; set; }
            [FromForm(Name = "description")] public string? Description { get; set; }
            [FromForm(Name = "icon_key")] public string? IconKey { get; set; }
            [FromForm(Name = "image")] public IFormFile? Image { get; set; }
            [FromForm(Name = "order")] public int? Order { get; set; }
            [FromForm(Name = "is_active")] public bool? IsActive { get; set; }
        }

        public class AchieverDto
        {
            [FromForm(Name = "name")] public string Name { get; set; }
            [FromForm(Name = "achievement")] public string? Achievement { get; set; }
            [FromForm(Name = "goal")] public string? Goal { get; set; }
            [FromForm(Name = "year")] public int? Year { get; set; }
            [FromForm(Name = "is_featured")] public bool? IsFeatured { get; set; }
            [FromForm(Name = "order")] public int? Order { get; set; }
            [FromForm(Name = "is_active")] public bool? IsActive { get; set; }
            [FromForm(Name = "photo")] public IFormFile? Photo { get; set; }
        }
    }
}
