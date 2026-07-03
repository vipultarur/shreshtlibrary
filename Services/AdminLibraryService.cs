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
        Task<ServiceResult<object>> GetPendingReviews(CancellationToken ct = default);
        Task<ServiceResult<object>> ApproveReview(long id, CancellationToken ct = default);
        Task<ServiceResult<object>> RejectReview(long id, string reason, CancellationToken ct = default);
        Task<ServiceResult<object>> DeleteReview(long id, CancellationToken ct = default);
        Task<ServiceResult<object>> GetReviewSummary(CancellationToken ct = default);
        Task<ServiceResult<object>> GetGalleryImages(CancellationToken ct = default);
        Task<ServiceResult<object>> UploadGalleryImage(AdminLibraryController.GalleryImageDto dto, CancellationToken ct = default);
        Task<ServiceResult<object>> DeleteGalleryImage(long id, CancellationToken ct = default);
    }

    public class AdminLibraryService : IAdminLibraryService
    {
        private readonly ApplicationDbContext _context;
        private readonly string _mediaDir;

        public AdminLibraryService(ApplicationDbContext context)
        {
            _context = context;
            var isDev = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
            _mediaDir = isDev 
                ? Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "shreshtlibrary", "media", "library"))
                : Path.Combine(Directory.GetCurrentDirectory(), "media", "library");
        }

        private async Task<string?> SaveImageAsync(IFormFile? file)
        {
            if (file == null) return null;
            if (!Directory.Exists(_mediaDir)) Directory.CreateDirectory(_mediaDir);
            var ext = Path.GetExtension(file.FileName);
            var fileName = $"img_{Guid.NewGuid()}{ext}";
            var relativePath = $"library/{fileName}";
            var filePath = Path.Combine(_mediaDir, fileName);
            
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var fileData = memoryStream.ToArray();

            // Save to disk
            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous))
            {
                await stream.WriteAsync(fileData, 0, fileData.Length);
            }

            // Save to DB
            try
            {
                var dbFile = new LibraryDatabasefile
                {
                    Name = relativePath,
                    Data = fileData,
                    ContentType = file.ContentType ?? "application/octet-stream",
                    CreatedAt = DateTime.UtcNow
                };
                _context.LibraryDatabasefiles.Add(dbFile);
            }
            catch
            {
                // Ignore DB save errors to not break the flow if table doesn't exist
            }

            return relativePath;
        }

        public async Task<ServiceResult<object>> GetLibraryInfo(CancellationToken ct = default)
        {
            var info = await _context.LibraryLibraryinfos.AsNoTracking().FirstOrDefaultAsync(ct);
            if (info == null)
            {
                info = new LibraryLibraryinfo
                {
                    LibraryName = "Shresht Library",
                    Logo = "",
                    Description = "Welcome to Shresht Library",
                    OwnerName = "Admin",
                    ContactNumber = "0000000000",
                    Email = "admin@shreshtlibrary.com",
                    OpeningTime = new TimeOnly(8, 0),
                    ClosingTime = new TimeOnly(22, 0),
                    TotalCapacity = 100,
                    AvailableSeats = 100,
                    AddressLine1 = "Library Address",
                    Area = "Area",
                    City = "City",
                    State = "State",
                    Country = "India",
                    PinCode = "000000",
                    Latitude = 0,
                    Longitude = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.LibraryLibraryinfos.Add(info);
                await _context.SaveChangesAsync(ct);
            }
            
            return ServiceResult<object>.Ok(new {
                library_name = info.LibraryName,
                logo = !string.IsNullOrEmpty(info.Logo) ? $"/media/{info.Logo}" : null,
                banner_image = !string.IsNullOrEmpty(info.BannerImage) ? $"/media/{info.BannerImage}" : null,
                description = info.Description,
                established_year = info.EstablishedYear,
                owner_name = info.OwnerName,
                contact_number = info.ContactNumber,
                email = info.Email,
                website = info.Website,
                opening_time = info.OpeningTime.ToString(@"HH\:mm"),
                closing_time = info.ClosingTime.ToString(@"HH\:mm"),
                weekly_off = info.WeeklyOff,
                total_capacity = info.TotalCapacity,
                available_seats = info.AvailableSeats,
                address_line1 = info.AddressLine1,
                address_line2 = info.AddressLine2,
                area = info.Area,
                city = info.City,
                state = info.State,
                country = info.Country,
                pin_code = info.PinCode,
                latitude = info.Latitude,
                longitude = info.Longitude,
                google_map_url = info.GoogleMapUrl,
                wifi = info.Wifi,
                ac = info.Ac,
                cctv = info.Cctv,
                drinking_water = info.DrinkingWater,
                lockers = info.Lockers,
                charging_points = info.ChargingPoints,
                parking = info.Parking,
                reading_area = info.ReadingArea,
                computer_access = info.ComputerAccess,
                printing = info.Printing,
                facebook_url = info.FacebookUrl,
                instagram_url = info.InstagramUrl,
                whatsapp_number = info.WhatsappNumber,
                telegram_url = info.TelegramUrl,
                youtube_url = info.YoutubeUrl,
                tagline = info.Tagline,
                mission = info.Mission,
                vision = info.Vision,
                history = info.History,
                welcome_message = info.WelcomeMessage,
                services = info.Services,
                courses_supported = info.CoursesSupported,
                statistics_description = info.StatisticsDescription,
                faq = info.Faq,
                testimonials = info.Testimonials,
                emergency_contact = info.EmergencyContact,
                footer_text = info.FooterText,
                membership_details = info.MembershipDetails,
                registration_process = info.RegistrationProcess,
                required_documents = info.RequiredDocuments,
                membership_benefits = info.MembershipBenefits,
                library_rules = info.LibraryRules,
                created_at = info.CreatedAt,
                updated_at = info.UpdatedAt
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

            if (dto.LibraryName != null) info.LibraryName = dto.LibraryName;
            if (dto.Description != null) info.Description = dto.Description;
            if (dto.EstablishedYear.HasValue) info.EstablishedYear = dto.EstablishedYear.Value;
            if (dto.OwnerName != null) info.OwnerName = dto.OwnerName;
            if (dto.ContactNumber != null) info.ContactNumber = dto.ContactNumber;
            if (dto.Email != null) info.Email = dto.Email;
            if (dto.Website != null) info.Website = dto.Website;
            
            if (!string.IsNullOrEmpty(dto.OpeningTime) && TimeOnly.TryParse(dto.OpeningTime, out var ot)) info.OpeningTime = ot;
            if (!string.IsNullOrEmpty(dto.ClosingTime) && TimeOnly.TryParse(dto.ClosingTime, out var closeT)) info.ClosingTime = closeT;
            
            if (dto.WeeklyOff != null) info.WeeklyOff = dto.WeeklyOff;
            if (dto.TotalCapacity.HasValue) info.TotalCapacity = dto.TotalCapacity.Value;
            if (dto.AvailableSeats.HasValue) info.AvailableSeats = dto.AvailableSeats.Value;
            if (dto.AddressLine1 != null) info.AddressLine1 = dto.AddressLine1;
            if (dto.AddressLine2 != null) info.AddressLine2 = dto.AddressLine2;
            if (dto.Area != null) info.Area = dto.Area;
            if (dto.City != null) info.City = dto.City;
            if (dto.State != null) info.State = dto.State;
            if (dto.Country != null) info.Country = dto.Country;
            if (dto.PinCode != null) info.PinCode = dto.PinCode;
            if (dto.Latitude.HasValue) info.Latitude = dto.Latitude.Value;
            if (dto.Longitude.HasValue) info.Longitude = dto.Longitude.Value;
            if (dto.GoogleMapUrl != null) info.GoogleMapUrl = dto.GoogleMapUrl;

            if (dto.Wifi.HasValue) info.Wifi = dto.Wifi.Value;
            if (dto.Ac.HasValue) info.Ac = dto.Ac.Value;
            if (dto.Cctv.HasValue) info.Cctv = dto.Cctv.Value;
            if (dto.DrinkingWater.HasValue) info.DrinkingWater = dto.DrinkingWater.Value;
            if (dto.Lockers.HasValue) info.Lockers = dto.Lockers.Value;
            if (dto.ChargingPoints.HasValue) info.ChargingPoints = dto.ChargingPoints.Value;
            if (dto.Parking.HasValue) info.Parking = dto.Parking.Value;
            if (dto.ReadingArea.HasValue) info.ReadingArea = dto.ReadingArea.Value;
            if (dto.ComputerAccess.HasValue) info.ComputerAccess = dto.ComputerAccess.Value;
            if (dto.Printing.HasValue) info.Printing = dto.Printing.Value;

            if (dto.FacebookUrl != null) info.FacebookUrl = dto.FacebookUrl;
            if (dto.InstagramUrl != null) info.InstagramUrl = dto.InstagramUrl;
            if (dto.WhatsappNumber != null) info.WhatsappNumber = dto.WhatsappNumber;
            if (dto.TelegramUrl != null) info.TelegramUrl = dto.TelegramUrl;
            if (dto.YoutubeUrl != null) info.YoutubeUrl = dto.YoutubeUrl;

            if (dto.Tagline != null) info.Tagline = dto.Tagline;
            if (dto.Mission != null) info.Mission = dto.Mission;
            if (dto.Vision != null) info.Vision = dto.Vision;
            if (dto.History != null) info.History = dto.History;
            if (dto.WelcomeMessage != null) info.WelcomeMessage = dto.WelcomeMessage;
            if (dto.Services != null) info.Services = dto.Services;
            if (dto.CoursesSupported != null) info.CoursesSupported = dto.CoursesSupported;
            if (dto.StatisticsDescription != null) info.StatisticsDescription = dto.StatisticsDescription;
            if (dto.Faq != null) info.Faq = dto.Faq;
            if (dto.Testimonials != null) info.Testimonials = dto.Testimonials;
            if (dto.EmergencyContact != null) info.EmergencyContact = dto.EmergencyContact;
            if (dto.FooterText != null) info.FooterText = dto.FooterText;
            if (dto.MembershipDetails != null) info.MembershipDetails = dto.MembershipDetails;
            if (dto.RegistrationProcess != null) info.RegistrationProcess = dto.RegistrationProcess;
            if (dto.RequiredDocuments != null) info.RequiredDocuments = dto.RequiredDocuments;
            if (dto.MembershipBenefits != null) info.MembershipBenefits = dto.MembershipBenefits;
            if (dto.LibraryRules != null) info.LibraryRules = dto.LibraryRules;

            if (dto.Logo != null) info.Logo = await SaveImageAsync(dto.Logo) ?? info.Logo;
            if (dto.BannerImage != null) info.BannerImage = await SaveImageAsync(dto.BannerImage) ?? info.BannerImage;

            info.UpdatedAt = DateTime.UtcNow;

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
            var reviews = await _context.LibraryReviews.AsNoTracking().Include(r => r.Student).OrderByDescending(r => r.CreatedAt).ToListAsync(ct);
            var data = reviews.Select(r => new {
                id = r.Id,
                student_id = r.StudentId,
                student_name = r.Student != null ? r.Student.FirstName + " " + r.Student.LastName : "Unknown",
                rating = r.Rating,
                comment = r.Comment,
                is_published = r.IsApproved,
                created_at = r.CreatedAt
            });
            return ServiceResult<object>.Ok(data);
        }

        public async Task<ServiceResult<object>> GetPendingReviews(CancellationToken ct = default)
        {
            var reviews = await _context.LibraryReviews.AsNoTracking().Include(r => r.Student).Where(r => r.IsApproved == false).OrderByDescending(r => r.CreatedAt).ToListAsync(ct);
            var data = reviews.Select(r => new {
                id = r.Id,
                student_id = r.StudentId,
                student_name = r.Student != null ? r.Student.FirstName + " " + r.Student.LastName : "Unknown",
                rating = r.Rating,
                comment = r.Comment,
                is_published = r.IsApproved,
                created_at = r.CreatedAt
            });
            return ServiceResult<object>.Ok(data);
        }

        public async Task<ServiceResult<object>> ApproveReview(long id, CancellationToken ct = default)
        {
            var review = await _context.LibraryReviews.FindAsync(new object[] { id }, ct);
            if (review == null) return ServiceResult<object>.NotFound("Review not found");
            
            review.IsApproved = true;
            review.RejectionReason = null;
            review.ApprovedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync(ct);
            return ServiceResult<object>.Ok("Review approved");
        }

        public async Task<ServiceResult<object>> RejectReview(long id, string reason, CancellationToken ct = default)
        {
            var review = await _context.LibraryReviews.FindAsync(new object[] { id }, ct);
            if (review == null) return ServiceResult<object>.NotFound("Review not found");
            
            review.IsApproved = false;
            review.RejectionReason = reason;
            
            await _context.SaveChangesAsync(ct);
            return ServiceResult<object>.Ok("Review rejected");
        }

        public async Task<ServiceResult<object>> DeleteReview(long id, CancellationToken ct = default)
        {
            var review = await _context.LibraryReviews.FindAsync(new object[] { id }, ct);
            if (review == null) return ServiceResult<object>.NotFound("Review not found");
            
            _context.LibraryReviews.Remove(review);
            await _context.SaveChangesAsync(ct);
            return ServiceResult<object>.Ok("Review deleted");
        }

        public async Task<ServiceResult<object>> GetReviewSummary(CancellationToken ct = default)
        {
            var count = await _context.LibraryReviews.CountAsync(ct);
            return ServiceResult<object>.Ok(new { count });
        }

        public async Task<ServiceResult<object>> GetGalleryImages(CancellationToken ct = default)
        {
            var images = await _context.LibraryGalleryImages.AsNoTracking().OrderBy(i => i.Order).ThenByDescending(i => i.CreatedAt).ToListAsync(ct);
            var data = images.Select(i => new {
                id = i.Id,
                image_url = !string.IsNullOrEmpty(i.ImageUrl) ? $"/media/{i.ImageUrl}" : null,
                caption = i.Caption,
                order = i.Order,
                created_at = i.CreatedAt
            });
            return ServiceResult<object>.Ok(data);
        }

        public async Task<ServiceResult<object>> UploadGalleryImage(AdminLibraryController.GalleryImageDto dto, CancellationToken ct = default)
        {
            var imagePath = await SaveImageAsync(dto.Image);
            if (string.IsNullOrEmpty(imagePath)) return ServiceResult<object>.Fail("Failed to upload image.");

            var galleryImage = new LibraryGalleryImage
            {
                ImageUrl = imagePath,
                Caption = dto.Caption,
                Order = dto.Order ?? 0,
                CreatedAt = DateTime.UtcNow
            };
            _context.LibraryGalleryImages.Add(galleryImage);
            await _context.SaveChangesAsync(ct);

            return ServiceResult<object>.Ok("Gallery image uploaded.");
        }

        public async Task<ServiceResult<object>> DeleteGalleryImage(long id, CancellationToken ct = default)
        {
            var image = await _context.LibraryGalleryImages.FindAsync(new object[] { id }, ct);
            if (image == null) return ServiceResult<object>.NotFound("Image not found.");

            _context.LibraryGalleryImages.Remove(image);
            await _context.SaveChangesAsync(ct);
            
            return ServiceResult<object>.Ok("Gallery image deleted.");
        }
    }
}
