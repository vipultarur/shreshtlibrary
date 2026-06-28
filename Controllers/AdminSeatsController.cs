using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.Authorization;
using WebApplication1.Services;
using System.Collections.Generic;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/v1/admin")]
    [Authorize(Roles = "admin,super_admin")]
    public class AdminSeatsController : ControllerBase
    {
        private readonly IAdminSeatService _adminSeatService;

        public AdminSeatsController(IAdminSeatService adminSeatService)
        {
            _adminSeatService = adminSeatService;
        }

        public class SeatCreateDto
        {
            [System.ComponentModel.DataAnnotations.Required]
            [System.Text.Json.Serialization.JsonPropertyName("floor")]
            public string Floor { get; set; }
            [System.ComponentModel.DataAnnotations.Required]
            [System.Text.Json.Serialization.JsonPropertyName("row")]
            public string Row { get; set; }
            [System.ComponentModel.DataAnnotations.Required]
            [System.Text.Json.Serialization.JsonPropertyName("seat_number")]
            public string SeatNumber { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("status")]
            public string? Status { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("notes")]
            public string? Notes { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("is_reserved_for_girls")]
            public bool? IsReservedForGirls { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("row_ref_id")]
            public long? RowRefId { get; set; }
        }

        public class FloorCreateDto
        {
            [System.Text.Json.Serialization.JsonPropertyName("name")]
            public string Name { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("description")]
            public string? Description { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("order")]
            public int Order { get; set; }
        }

        public class RowCreateDto
        {
            [System.Text.Json.Serialization.JsonPropertyName("floor_id")]
            public long FloorId { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("label")]
            public string Label { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("order")]
            public int Order { get; set; }
        }

        public class SeatUpdateStatusDto
        {
            [System.Text.Json.Serialization.JsonPropertyName("status")]
            public string Status { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("reason")]
            public string? Reason { get; set; }
        }

        public class SeatAssignDto
        {
            [System.Text.Json.Serialization.JsonPropertyName("student_id")]
            public string StudentId { get; set; }
        }

        public class SeatUnassignDto
        {
            [System.Text.Json.Serialization.JsonPropertyName("reason")]
            public string? Reason { get; set; }
        }

        public class SeatReserveBulkDto
        {
            [System.Text.Json.Serialization.JsonPropertyName("seat_ids")]
            public List<int> SeatIds { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("is_reserved_for_girls")]
            public bool IsReservedForGirls { get; set; }
        }

        [HttpGet("seats/layout")]
        public async Task<IActionResult> SeatsLayoutAsync(CancellationToken ct)
        {
            var result = await _adminSeatService.GetSeatsLayoutAsync(ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpPost("seats/release-all")]
        public async Task<IActionResult> SeatsReleaseAllAsync(CancellationToken ct)
        {
            var result = await _adminSeatService.ReleaseAllSeatsAsync(ct);
            var dynamicData = (dynamic)result.Data;
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(new { }, dynamicData?.message));
        }
        
        [HttpPost("seats/reserve-bulk")]
        public async Task<IActionResult> SeatsReserveBulkAsync([FromBody] SeatReserveBulkDto dto, CancellationToken ct)
        {
            var result = await _adminSeatService.ReserveBulkSeatsAsync(dto.SeatIds, dto.IsReservedForGirls, ct);
            if (!result.Success) return BadRequest(new { success = false, message = result.Message });
            
            var dynamicData = (dynamic)result.Data;
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(new { }, dynamicData?.message));
        }

        [HttpGet("seats/available")]
        public async Task<IActionResult> SeatsAvailableAsync(CancellationToken ct)
        {
            var result = await _adminSeatService.GetAvailableSeatsAsync(ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("seats/stats")]
        public async Task<IActionResult> SeatsStatsAsync(CancellationToken ct)
        {
            var result = await _adminSeatService.GetSeatsStatsAsync(ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpPost("seats")]
        public async Task<IActionResult> SeatsAddAsync([FromBody] SeatCreateDto dto, CancellationToken ct)
        {
            var result = await _adminSeatService.AddSeatAsync(dto.Floor, dto.Row, dto.SeatNumber, dto.Status, dto.Notes, dto.IsReservedForGirls, dto.RowRefId, ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpDelete("seats/{pk}")]
        public async Task<IActionResult> SeatDeleteAsync(int pk, CancellationToken ct)
        {
            var result = await _adminSeatService.DeleteSeatAsync(pk, ct);
            if (result.IsNotFound) return NotFound(new { success = false, message = result.Message });
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(new { }));
        }

        [HttpGet("seats")]
        public async Task<IActionResult> SeatsListAsync(CancellationToken ct, [FromQuery] int page = 1, [FromQuery] int page_size = 200)
        {
            var nextTemplate = $"{Request.Scheme}://{Request.Host}{Request.Path}?page={{P}}&page_size={page_size}";
            var prevTemplate = $"{Request.Scheme}://{Request.Host}{Request.Path}?page={{P}}&page_size={page_size}";
            
            var result = await _adminSeatService.GetSeatsListAsync(page, page_size, nextTemplate, prevTemplate, ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("seats/{pk}")]
        public async Task<IActionResult> SeatDetailAsync(int pk, CancellationToken ct)
        {
            var result = await _adminSeatService.GetSeatDetailAsync(pk, ct);
            if (result.IsNotFound) return NotFound(new { success = false, message = result.Message });
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpPut("seats/{pk}")]
        public async Task<IActionResult> SeatUpdateAsync(int pk, [FromBody] SeatCreateDto dto, CancellationToken ct)
        {
            var result = await _adminSeatService.UpdateSeatAsync(pk, dto.Floor, dto.Row, dto.SeatNumber, dto.Status, dto.Notes, dto.IsReservedForGirls, dto.RowRefId, ct);
            if (result.IsNotFound) return NotFound(new { success = false, message = result.Message });
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpPatch("seats/{pk}/status")]
        public async Task<IActionResult> SeatStatusAsync(int pk, [FromBody] SeatUpdateStatusDto dto, CancellationToken ct)
        {
            var result = await _adminSeatService.UpdateSeatStatusAsync(pk, dto.Status, dto.Reason, ct);
            if (result.IsNotFound) return NotFound(new { success = false, message = result.Message });
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpPost("seats/{pk}/assign")]
        public async Task<IActionResult> SeatAssignAsync(int pk, [FromBody] SeatAssignDto dto, CancellationToken ct)
        {
            if (long.TryParse(dto.StudentId?.ToString(), out var sid)) {
                var result = await _adminSeatService.AssignSeatAsync(pk, sid, ct);
                if (result.IsNotFound) return NotFound(new { success = false, message = result.Message });
                return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
            }
            return NotFound(new { success = false, message = "Seat not found" });
        }

        [HttpPost("seats/{pk}/unassign")]
        public async Task<IActionResult> SeatUnassignAsync(int pk, [FromBody] SeatUnassignDto dto, CancellationToken ct)
        {
            var result = await _adminSeatService.UnassignSeatAsync(pk, dto.Reason, ct);
            if (result.IsNotFound) return NotFound(new { success = false, message = result.Message });
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("seats/{pk}/history")]
        public async Task<IActionResult> SeatHistoryAsync(int pk, CancellationToken ct)
        {
            var result = await _adminSeatService.GetSeatHistoryAsync(pk, ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("floors")]
        public async Task<IActionResult> FloorsListAsync(CancellationToken ct)
        {
            var result = await _adminSeatService.GetFloorsListAsync(ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("floors/{pk}")]
        public async Task<IActionResult> FloorDetailAsync(int pk, CancellationToken ct)
        {
            var result = await _adminSeatService.GetFloorDetailAsync(pk, ct);
            if (result.IsNotFound) return NotFound(new { success = false, message = result.Message });
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpPost("floors")]
        public async Task<IActionResult> FloorAddAsync([FromBody] FloorCreateDto dto, CancellationToken ct)
        {
            var result = await _adminSeatService.AddFloorAsync(dto.Name, dto.Description, dto.Order, ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpDelete("floors/{pk}")]
        public async Task<IActionResult> FloorDeleteAsync(int pk, CancellationToken ct)
        {
            var result = await _adminSeatService.DeleteFloorAsync(pk, ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(new { }));
        }

        [HttpGet("rows")]
        public async Task<IActionResult> RowsListAsync(CancellationToken ct)
        {
            var result = await _adminSeatService.GetRowsListAsync(ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpGet("rows/{pk}")]
        public async Task<IActionResult> RowDetailAsync(int pk, CancellationToken ct)
        {
            var result = await _adminSeatService.GetRowDetailAsync(pk, ct);
            if (result.IsNotFound) return NotFound(new { success = false, message = result.Message });
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpPost("rows")]
        public async Task<IActionResult> RowAddAsync([FromBody] RowCreateDto dto, CancellationToken ct)
        {
            var result = await _adminSeatService.AddRowAsync(dto.FloorId, dto.Label, dto.Order, ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpDelete("rows/{pk}")]
        public async Task<IActionResult> RowDeleteAsync(int pk, CancellationToken ct)
        {
            var result = await _adminSeatService.DeleteRowAsync(pk, ct);
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(new { }));
        }

        [HttpPut("floors/{pk}")]
        public async Task<IActionResult> FloorUpdateAsync(int pk, [FromBody] FloorCreateDto dto, CancellationToken ct)
        {
            var result = await _adminSeatService.UpdateFloorAsync(pk, dto.Name, dto.Description, dto.Order, ct);
            if (result.IsNotFound) return NotFound(new { success = false, message = result.Message });
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }

        [HttpPut("rows/{pk}")]
        public async Task<IActionResult> RowUpdateAsync(int pk, [FromBody] RowCreateDto dto, CancellationToken ct)
        {
            var result = await _adminSeatService.UpdateRowAsync(pk, dto.FloorId, dto.Label, dto.Order, ct);
            if (result.IsNotFound) return NotFound(new { success = false, message = result.Message });
            return Ok(WebApplication1.Models.Responses.ApiResponse<object>.Ok(result.Data));
        }
    }
}
