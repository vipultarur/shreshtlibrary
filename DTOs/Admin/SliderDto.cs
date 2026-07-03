using Microsoft.AspNetCore.Http;

using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.DTOs.Admin
{
    public class SliderDto
    {
        [FromForm(Name = "title")] public string Title { get; set; } = string.Empty;
        [FromForm(Name = "subtitle")] public string? Subtitle { get; set; }
        [FromForm(Name = "image")] public IFormFile? Image { get; set; }
        [FromForm(Name = "link_url")] public string? LinkUrl { get; set; }
        [FromForm(Name = "sort_order")] public int? SortOrder { get; set; }
        [FromForm(Name = "is_active")] public bool? IsActive { get; set; }
    }
}
