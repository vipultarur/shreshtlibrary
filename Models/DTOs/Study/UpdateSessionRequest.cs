namespace WebApplication1.Models.DTOs.Study
{
    public class UpdateSessionRequest
    {
        public string status { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Range(0, 1440, ErrorMessage = "Duration minutes cannot exceed 1440 (24 hours).")]
        public int? duration_minutes { get; set; }

        [System.ComponentModel.DataAnnotations.Range(0, 1440, ErrorMessage = "Paused minutes cannot exceed 1440.")]
        public int? paused_minutes { get; set; }
    }
}
