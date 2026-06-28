namespace WebApplication1.DTOs.Admin
{
    public record AdminProfileUpdateDto(string? first_name, string? last_name, string? email, string? mobile, Microsoft.AspNetCore.Http.IFormFile? profile_image);
}
