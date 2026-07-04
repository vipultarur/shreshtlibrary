namespace WebApplication1.Models.DTOs.Library
{
    public class SubmitReviewRequest
    {
        public int rating { get; set; }
        public string comment { get; set; } = string.Empty;
    }
}
