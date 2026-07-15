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
        private readonly ICloudinaryService _cloudinary;

        public AdminSlidersService(IRepository<LibraryHomeslider> repository, ICloudinaryService cloudinary)
        {
            _repository = repository;
            _cloudinary = cloudinary;
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
                    image = !string.IsNullOrEmpty(s.Image) ? (s.Image.StartsWith("http") ? s.Image : $"/media/{s.Image}") : null,
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
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var ext = System.IO.Path.GetExtension(dto.Image.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(ext)) return ServiceResult<object>.Fail("Invalid image format.");

                slider.Image = await _cloudinary.UploadImageAsync(dto.Image, "sliders");
            }

            _repository.Add(slider);
            await _repository.SaveChangesAsync(ct);

            return ServiceResult<object>.Ok(new
            {
                id = slider.Id,
                title = slider.Title,
                subtitle = slider.Subtitle,
                image = !string.IsNullOrEmpty(slider.Image) ? (slider.Image.StartsWith("http") ? slider.Image : $"/media/{slider.Image}") : null,
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
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var ext = System.IO.Path.GetExtension(dto.Image.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(ext)) return ServiceResult<object>.Fail("Invalid image format.");

                var newImage = await _cloudinary.UploadImageAsync(dto.Image, "sliders");
                if (!string.IsNullOrEmpty(newImage)) 
                {
                    slider.Image = newImage;
                }
            }

            _repository.Update(slider);
            await _repository.SaveChangesAsync(ct);

            return ServiceResult<object>.Ok(new
            {
                id = slider.Id,
                title = slider.Title,
                subtitle = slider.Subtitle,
                image = !string.IsNullOrEmpty(slider.Image) ? (slider.Image.StartsWith("http") ? slider.Image : $"/media/{slider.Image}") : null,
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
