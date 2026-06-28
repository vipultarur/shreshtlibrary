using Microsoft.AspNetCore.Http;

namespace WebApplication1.DTOs.Admin
{
    public class SliderDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public IFormFile? Image { get; set; }
        public string? LinkUrl { get; set; }
        public int? SortOrder { get; set; }
        public bool? IsActive { get; set; }
    }
}
