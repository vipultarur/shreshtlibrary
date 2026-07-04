namespace WebApplication1.Models.DTOs.Admin
{
    public class PermissionPayload
    {
        public long AdminId { get; set; }
        public string[]? Permissions { get; set; }
    }
}
