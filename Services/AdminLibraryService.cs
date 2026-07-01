using Microsoft.EntityFrameworkCore;
using WebApplication1.Controllers;
using WebApplication1.Data;
using WebApplication1.Models;
using System.IO;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.Http;

namespace WebApplication1.Services
{
    public interface IAdminLibraryService
    {
        Task<ServiceResult<object>> GetLibraryInfo(CancellationToken ct = default);
        Task<ServiceResult<object>> UpdateLibraryInfo(AdminLibraryController.LibraryInfoUpdateDto dto, CancellationToken ct = default);
        Task<ServiceResult<object>> GetFacilities(CancellationToken ct = default);
        Task<ServiceResult<object>> CreateFacility(AdminLibraryController.FacilityDto dto, CancellationToken ct = default);
        Task<ServiceResult<object>> UpdateFacility(long id, AdminLibraryController.FacilityDto dto, CancellationToken ct = default);
        Task<ServiceResult<object>> ToggleFacility(long id, CancellationToken ct = default);
        Task<ServiceResult<object>> DeleteFacility(long id, CancellationToken ct = default);
        Task<ServiceResult<object>> GetAchievers(CancellationToken ct = default);
        Task<ServiceResult<object>> CreateAchiever(AdminLibraryController.AchieverDto dto, CancellationToken ct = default);
        Task<ServiceResult<object>> UpdateAchiever(long id, AdminLibraryController.AchieverDto dto, CancellationToken ct = default);
        Task<ServiceResult<object>> ToggleAchiever(long id, CancellationToken ct = default);
        Task<ServiceResult<object>> DeleteAchiever(long id, CancellationToken ct = default);
        Task<ServiceResult<object>> GetReviews(CancellationToken ct = default);
        Task<ServiceResult<object>> GetReviewSummary(CancellationToken ct = default);
    }

    public class AdminLibraryService : IAdminLibraryService
    {
        private readonly ApplicationDbContext _context;
        private readonly string _mediaDir;

        public AdminLibraryService(ApplicationDbContext context)
        {
            _context = context;
            _mediaDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "shreshtlibrary", "media", "library"));
        }

        private async Task<string?> SaveImageAsync(IFormFile? file)
        {
            if (file == null) return null;
            if (!Directory.Exists(_mediaDir)) Directory.CreateDirectory(_mediaDir);
            var ext = Path.GetExtension(file.FileName);
            var fileName = $"img_{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(_mediaDir, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous))
            {
                await file.CopyToAsync(stream);
            }
            return $"library/{fileName}";
        }

        public async Task<ServiceResult<object>> GetLibraryInfo(CancellationToken ct = default)
        {
            var info = await _context.LibraryLibraryinfos.AsNoTracking().FirstOrDefaultAsync(ct);
            if (info == null) return ServiceResult<object>.Ok(new { });
            
            return ServiceResult<object>.Ok(new {
                id = info.Id,
                name = info.Name,
                tagline = info.Tagline,
                phone_primary = info.PhonePrimary,
                phone_secondary = info.PhoneSecondary,
                email = info.Email,
                address = info.Address,
                google_maps_url = info.GoogleMapsUrl,
                facebook_url = info.FacebookUrl,
                instagram_url = info.InstagramUrl,
                website = info.Website,
                open_time = info.OpenTime?.ToString(@"HH\:mm"),
                close_time = info.CloseTime?.ToString(@"HH\:mm"),
                off_days = info.OffDays,
                about = info.About,
                description = info.Description,
                rules = info.Rules,
                facilities = info.Facilities,
                feature_image = !string.IsNullOrEmpty(info.FeatureImage) ? $"/media/{info.FeatureImage}" : null,
                logo_square = !string.IsNullOrEmpty(info.LogoSquare) ? $"/media/{info.LogoSquare}" : null,
                logo_rectangle = !string.IsNullOrEmpty(info.LogoRectangle) ? $"/media/{info.LogoRectangle}" : null
            });
        }

        public async Task<ServiceResult<object>> UpdateLibraryInfo(AdminLibraryController.LibraryInfoUpdateDto dto, CancellationToken ct = default)
        {
            var info = await _context.LibraryLibraryinfos.FirstOrDefaultAsync(ct);
            if (info == null)
            {
                info = new LibraryLibraryinfo();
                _context.LibraryLibraryinfos.Add(info);
            }

            if (dto.Name != null) info.Name = dto.Name;
            if (dto.Tagline != null) info.Tagline = dto.Tagline;
            if (dto.PhonePrimary != null) info.PhonePrimary = dto.PhonePrimary;
            if (dto.PhoneSecondary != null) info.PhoneSecondary = dto.PhoneSecondary;
            if (dto.Email != null) info.Email = dto.Email;
            if (dto.Address != null) info.Address = dto.Address;
            if (dto.GoogleMapsUrl != null) info.GoogleMapsUrl = dto.GoogleMapsUrl;
            if (dto.FacebookUrl != null) info.FacebookUrl = dto.FacebookUrl;
            if (dto.InstagramUrl != null) info.InstagramUrl = dto.InstagramUrl;
            if (dto.Website != null) info.Website = dto.Website;
            
            if (!string.IsNullOrEmpty(dto.OpenTime) && TimeOnly.TryParse(dto.OpenTime, out var ot)) info.OpenTime = ot;
            if (!string.IsNullOrEmpty(dto.CloseTime) && TimeOnly.TryParse(dto.CloseTime, out var closeT)) info.CloseTime = closeT;
            
            if (dto.OffDays != null) info.OffDays = dto.OffDays;
            if (dto.About != null) info.About = dto.About;
            if (dto.Description != null) info.Description = dto.Description;
            if (dto.Rules != null) info.Rules = dto.Rules;
            if (dto.Facilities != null) info.Facilities = dto.Facilities;

            if (dto.FeatureImage != null) info.FeatureImage = await SaveImageAsync(dto.FeatureImage) ?? info.FeatureImage;
            if (dto.LogoSquare != null) info.LogoSquare = await SaveImageAsync(dto.LogoSquare) ?? info.LogoSquare;
            if (dto.LogoRectangle != null) info.LogoRectangle = await SaveImageAsync(dto.LogoRectangle) ?? info.LogoRectangle;

            await _context.SaveChangesAsync(ct);
            return ServiceResult<object>.Ok("Library info updated.");
        }

        public async Task<ServiceResult<object>> GetFacilities(CancellationToken ct = default)
        {
            var facilities = await _context.LibraryFacilities.AsNoTracking().OrderBy(f => f.Order).ToListAsync(ct);
            var data = facilities.Select(f => new {
                id = f.Id,
                name = f.Name,
                description = f.Description,
                image = !string.IsNullOrEmpty(f.Image) ? $"/media/{f.Image}" : null,
                icon_key = f.IconKey,
                order = f.Order,
                is_active = f.IsActive
            });
            return ServiceResult<object>.Ok(data);
        }

        public async Task<ServiceResult<object>> CreateFacility(AdminLibraryController.FacilityDto dto, CancellationToken ct = default)
        {
            var iconPath = await SaveImageAsync(dto.Image);
            var facility = new LibraryFacility
            {
                Name = dto.Name,
                Description = dto.Description ?? "",
                Image = iconPath ?? "",
                IconKey = dto.IconKey ?? "default",
                Order = dto.Order ?? 0,
                IsActive = dto.IsActive ?? true
            };
            _context.LibraryFacilities.Add(facility);
            await _context.SaveChangesAsync(ct);
            return ServiceResult<object>.Ok("Facility created.");
        }

        public async Task<ServiceResult<object>> UpdateFacility(long id, AdminLibraryController.FacilityDto dto, CancellationToken ct = default)
        {
            var facility = await _context.LibraryFacilities.FindAsync(new object[] { id }, ct);
            if (facility == null) return ServiceResult<object>.NotFound("Not found");

            if (dto.Name != null) facility.Name = dto.Name;
            if (dto.Description != null) facility.Description = dto.Description;
            if (dto.IconKey != null) facility.IconKey = dto.IconKey;
            if (dto.Order.HasValue) facility.Order = dto.Order.Value;
            if (dto.IsActive.HasValue) facility.IsActive = dto.IsActive.Value;
            if (dto.Image != null)
            {
                facility.Image = await SaveImageAsync(dto.Image) ?? facility.Image;
            }

            await _context.SaveChangesAsync(ct);
            return ServiceResult<object>.Ok("Facility updated.");
        }

        public async Task<ServiceResult<object>> ToggleFacility(long id, CancellationToken ct = default)
        {
            var facility = await _context.LibraryFacilities.FindAsync(new object[] { id }, ct);
            if (facility == null) return ServiceResult<object>.NotFound("Not found");
            facility.IsActive = !facility.IsActive;
            await _context.SaveChangesAsync(ct);
            return ServiceResult<object>.Ok("Facility toggled.");
        }

        public async Task<ServiceResult<object>> DeleteFacility(long id, CancellationToken ct = default)
        {
            var facility = await _context.LibraryFacilities.FindAsync(new object[] { id }, ct);
            if (facility == null) return ServiceResult<object>.NotFound("Not found");
            _context.LibraryFacilities.Remove(facility);
            await _context.SaveChangesAsync(ct);
            return ServiceResult<object>.Ok("Facility deleted.");
        }

        public async Task<ServiceResult<object>> GetAchievers(CancellationToken ct = default)
        {
            var achievers = await _context.LibraryAchievers.AsNoTracking().OrderBy(a => a.Order).ToListAsync(ct);
            var data = achievers.Select(a => new {
                id = a.Id,
                name = a.Name,
                achievement = a.Achievement,
                goal = a.Goal,
                year = a.Year,
                is_featured = a.IsFeatured,
                photo = !string.IsNullOrEmpty(a.Photo) ? $"/media/{a.Photo}" : null,
                order = a.Order,
                is_active = a.IsActive
            });
            return ServiceResult<object>.Ok(data);
        }

        public async Task<ServiceResult<object>> CreateAchiever(AdminLibraryController.AchieverDto dto, CancellationToken ct = default)
        {
            var imagePath = await SaveImageAsync(dto.Photo);
            var achiever = new LibraryAchiever
            {
                Name = dto.Name,
                Achievement = dto.Achievement ?? "",
                Goal = dto.Goal ?? "",
                Year = dto.Year ?? DateTime.Now.Year,
                IsFeatured = dto.IsFeatured ?? false,
                Photo = imagePath ?? "",
                Order = dto.Order ?? 0,
                IsActive = dto.IsActive ?? true
            };
            _context.LibraryAchievers.Add(achiever);
            await _context.SaveChangesAsync(ct);
            return ServiceResult<object>.Ok("Achiever created.");
        }

        public async Task<ServiceResult<object>> UpdateAchiever(long id, AdminLibraryController.AchieverDto dto, CancellationToken ct = default)
        {
            var achiever = await _context.LibraryAchievers.FindAsync(new object[] { id }, ct);
            if (achiever == null) return ServiceResult<object>.NotFound("Not found");

            if (dto.Name != null) achiever.Name = dto.Name;
            if (dto.Achievement != null) achiever.Achievement = dto.Achievement;
            if (dto.Goal != null) achiever.Goal = dto.Goal;
            if (dto.Year.HasValue) achiever.Year = dto.Year.Value;
            if (dto.IsFeatured.HasValue) achiever.IsFeatured = dto.IsFeatured.Value;
            if (dto.Order.HasValue) achiever.Order = dto.Order.Value;
            if (dto.IsActive.HasValue) achiever.IsActive = dto.IsActive.Value;
            if (dto.Photo != null)
            {
                achiever.Photo = await SaveImageAsync(dto.Photo) ?? achiever.Photo;
            }

            await _context.SaveChangesAsync(ct);
            return ServiceResult<object>.Ok("Achiever updated.");
        }

        public async Task<ServiceResult<object>> ToggleAchiever(long id, CancellationToken ct = default)
        {
            var achiever = await _context.LibraryAchievers.FindAsync(new object[] { id }, ct);
            if (achiever == null) return ServiceResult<object>.NotFound("Not found");
            achiever.IsActive = !achiever.IsActive;
            await _context.SaveChangesAsync(ct);
            return ServiceResult<object>.Ok("Achiever toggled.");
        }

        public async Task<ServiceResult<object>> DeleteAchiever(long id, CancellationToken ct = default)
        {
            var achiever = await _context.LibraryAchievers.FindAsync(new object[] { id }, ct);
            if (achiever == null) return ServiceResult<object>.NotFound("Not found");
            _context.LibraryAchievers.Remove(achiever);
            await _context.SaveChangesAsync(ct);
            return ServiceResult<object>.Ok("Achiever deleted.");
        }

        public async Task<ServiceResult<object>> GetReviews(CancellationToken ct = default)
        {
            var reviews = await _context.LibraryReviews.AsNoTracking().OrderByDescending(r => r.CreatedAt).ToListAsync(ct);
            var data = reviews.Select(r => new {
                id = r.Id,
                student_id = r.StudentId,
                rating = r.Rating,
                comment = r.Comment,
                is_published = r.IsApproved,
                created_at = r.CreatedAt
            });
            return ServiceResult<object>.Ok(data);
        }

        public async Task<ServiceResult<object>> GetReviewSummary(CancellationToken ct = default)
        {
            var count = await _context.LibraryReviews.CountAsync(ct);
            return ServiceResult<object>.Ok(new { count });
        }
    }
}