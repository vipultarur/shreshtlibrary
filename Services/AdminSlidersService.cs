using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.DTOs.Admin;
using WebApplication1.Models;
using WebApplication1.Repositories;

namespace WebApplication1.Services
{
    public class AdminSlidersService : IAdminSlidersService
    {
        private readonly IRepository<LibraryHomeslider> _repository;
        private readonly IRepository<LibraryDatabasefile> _fileRepository;

        public AdminSlidersService(IRepository<LibraryHomeslider> repository, IRepository<LibraryDatabasefile> fileRepository)
        {
            _repository = repository;
            _fileRepository = fileRepository;
        }

        public async Task<ServiceResult<object>> GetSliders(CancellationToken ct = default)
        {
            var sliders = await _repository.Query(trackChanges: false)
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
                    _fileRepository.Add(new LibraryDatabasefile
                    {
                        Name = relativePath,
                        Data = fileData,
                        ContentType = dto.Image.ContentType ?? "application/octet-stream",
                        CreatedAt = DateTime.UtcNow
                    });
                    await _fileRepository.SaveChangesAsync(ct);
                }
                catch (Exception ex) 
                {
                    Serilog.Log.Error(ex, "Failed to insert slider database file record");
                }

                slider.Image = $"sliders/{fileName}";
            }

            _repository.Add(slider);
            await _repository.SaveChangesAsync(ct);

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
            var slider = await _repository.GetByIdAsync(id, ct);
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
                    _fileRepository.Add(new LibraryDatabasefile
                    {
                        Name = relativePath,
                        Data = fileData,
                        ContentType = dto.Image.ContentType ?? "application/octet-stream",
                        CreatedAt = DateTime.UtcNow
                    });
                    await _fileRepository.SaveChangesAsync(ct);
                }
                catch (Exception ex) 
                {
                    Serilog.Log.Error(ex, "Failed to insert slider database file record");
                }

                slider.Image = $"sliders/{fileName}";
            }

            _repository.Update(slider);
            await _repository.SaveChangesAsync(ct);

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
            var slider = await _repository.GetByIdAsync(id, ct);
            if (slider == null) return ServiceResult<object>.NotFound("Slider not found.");

            _repository.Remove(slider);
            await _repository.SaveChangesAsync(ct);

            return ServiceResult<object>.Ok(new { message = "Slider deleted successfully." });
        }
    }
}
