using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.DTOs.Admin;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public class AdminSlidersService : IAdminSlidersService
    {
        private readonly ApplicationDbContext _context;

        public AdminSlidersService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceResult<object>> GetSliders(CancellationToken ct = default)
        {
            var sliders = await _context.LibraryHomesliders
                .AsNoTracking()
                .OrderBy(s => s.SortOrder)
                .Select(s => new
                {
                    id = s.Id,
                    title = s.Title,
                    subtitle = s.Subtitle,
                    image = !string.IsNullOrEmpty(s.Image) ? $"/media/{s.Image}" : null,
                    link_url = s.LinkUrl,
                    is_active = s.IsActive,
                    sort_order = s.SortOrder,
                    created_at = s.CreatedAt
                })
                .ToListAsync(ct);
            return ServiceResult<object>.Ok(sliders);
        }

        public async Task<ServiceResult<object>> CreateSlider(SliderDto dto, CancellationToken ct = default)
        {
            var slider = new LibraryHomeslider
            {
                Title = dto.Title,
                Subtitle = dto.Subtitle ?? "",
                LinkUrl = dto.LinkUrl ?? "",
                SortOrder = dto.SortOrder ?? 0,
                IsActive = dto.IsActive ?? true,
                CreatedAt = DateTime.UtcNow
            };

            if (dto.Image != null)
            {
                var isDev = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
                var mediaDir = isDev 
                    ? System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "..", "shreshtlibrary", "media", "sliders"))
                    : System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "media", "sliders");
                if (!System.IO.Directory.Exists(mediaDir)) System.IO.Directory.CreateDirectory(mediaDir);

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var ext = System.IO.Path.GetExtension(dto.Image.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(ext)) return ServiceResult<object>.Fail("Invalid image format.");

                var fileName = $"slider_{Guid.NewGuid()}{ext}";
                var relativePath = $"sliders/{fileName}";
                var filePath = System.IO.Path.Combine(mediaDir, fileName);

                using var memoryStream = new System.IO.MemoryStream();
                await dto.Image.CopyToAsync(memoryStream, ct);
                var fileData = memoryStream.ToArray();

                using (var stream = new System.IO.FileStream(filePath, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None, 4096, System.IO.FileOptions.Asynchronous))
                {
                    await stream.WriteAsync(fileData, 0, fileData.Length, ct);
                }

                try
                {
                    var sql = "INSERT INTO library_databasefile (name, data, content_type, created_at) VALUES (@p0, @p1, @p2, @p3)";
                    await _context.Database.ExecuteSqlRawAsync(sql, relativePath, fileData, dto.Image.ContentType ?? "application/octet-stream", DateTime.UtcNow);
                }
                catch {}

                slider.Image = $"sliders/{fileName}";
            }

            _context.LibraryHomesliders.Add(slider);
            await _context.SaveChangesAsync(ct);

            return ServiceResult<object>.Ok(new
            {
                id = slider.Id,
                title = slider.Title,
                subtitle = slider.Subtitle,
                image = !string.IsNullOrEmpty(slider.Image) ? $"/media/{slider.Image}" : null,
                link_url = slider.LinkUrl,
                is_active = slider.IsActive,
                sort_order = slider.SortOrder,
                created_at = slider.CreatedAt
            });
        }

        public async Task<ServiceResult<object>> UpdateSlider(long id, SliderDto dto, CancellationToken ct = default)
        {
            var slider = await _context.LibraryHomesliders.FirstOrDefaultAsync(s => s.Id == id, ct);
            if (slider == null) return ServiceResult<object>.NotFound("Slider not found.");

            if (dto.Title != null) slider.Title = dto.Title;
            if (dto.Subtitle != null) slider.Subtitle = dto.Subtitle;
            if (dto.LinkUrl != null) slider.LinkUrl = dto.LinkUrl;
            if (dto.SortOrder.HasValue) slider.SortOrder = dto.SortOrder.Value;
            if (dto.IsActive.HasValue) slider.IsActive = dto.IsActive.Value;

            if (dto.Image != null)
            {
                var isDev = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
                var mediaDir = isDev 
                    ? System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "..", "shreshtlibrary", "media", "sliders"))
                    : System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "media", "sliders");
                if (!System.IO.Directory.Exists(mediaDir)) System.IO.Directory.CreateDirectory(mediaDir);

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var ext = System.IO.Path.GetExtension(dto.Image.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(ext)) return ServiceResult<object>.Fail("Invalid image format.");

                var fileName = $"slider_{Guid.NewGuid()}{ext}";
                var relativePath = $"sliders/{fileName}";
                var filePath = System.IO.Path.Combine(mediaDir, fileName);

                using var memoryStream = new System.IO.MemoryStream();
                await dto.Image.CopyToAsync(memoryStream, ct);
                var fileData = memoryStream.ToArray();

                using (var stream = new System.IO.FileStream(filePath, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None, 4096, System.IO.FileOptions.Asynchronous))
                {
                    await stream.WriteAsync(fileData, 0, fileData.Length, ct);
                }

                try
                {
                    var sql = "INSERT INTO library_databasefile (name, data, content_type, created_at) VALUES (@p0, @p1, @p2, @p3)";
                    await _context.Database.ExecuteSqlRawAsync(sql, relativePath, fileData, dto.Image.ContentType ?? "application/octet-stream", DateTime.UtcNow);
                }
                catch {}

                slider.Image = $"sliders/{fileName}";
            }

            await _context.SaveChangesAsync(ct);

            return ServiceResult<object>.Ok(new
            {
                id = slider.Id,
                title = slider.Title,
                subtitle = slider.Subtitle,
                image = !string.IsNullOrEmpty(slider.Image) ? $"/media/{slider.Image}" : null,
                link_url = slider.LinkUrl,
                is_active = slider.IsActive,
                sort_order = slider.SortOrder,
                created_at = slider.CreatedAt
            });
        }

        public async Task<ServiceResult<object>> DeleteSlider(long id, CancellationToken ct = default)
        {
            var slider = await _context.LibraryHomesliders.FirstOrDefaultAsync(s => s.Id == id, ct);
            if (slider == null) return ServiceResult<object>.NotFound("Slider not found.");

            _context.LibraryHomesliders.Remove(slider);
            await _context.SaveChangesAsync(ct);

            return ServiceResult<object>.Ok(new { message = "Slider deleted successfully." });
        }
    }
}
