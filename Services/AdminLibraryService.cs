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
        Task<ServiceResult<object>> GetReviews(int page = 1, int pageSize = 20, CancellationToken ct = default);
        Task<ServiceResult<object>> GetPendingReviews(int page = 1, int pageSize = 20, CancellationToken ct = default);
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
        private readonly ICloudinaryService _cloudinary;

        public AdminLibraryService(ApplicationDbContext context, ICloudinaryService cloudinary)
        {
            _context = context;
            _cloudinary = cloudinary;
        }



        public async Task<ServiceResult<object>> GetLibraryInfo(CancellationToken ct = default)
        {
            try
            {
                var info = await _context.LibraryLibraryinfos.AsNoTracking().FirstOrDefaultAsync(ct);
                if (info == null)
                {
                    return ServiceResult<object>.Ok(new { message = "Library information not configured yet." });
                }

                return ServiceResult<object>.Ok(new
                {
                    id = info.Id,
                    library_name = info.LibraryName,
                    logo = !string.IsNullOrEmpty(info.Logo) ? (info.Logo.StartsWith("http") ? info.Logo : $"/media/{info.Logo}") : null,
                    banner_image = !string.IsNullOrEmpty(info.BannerImage) ? (info.BannerImage.StartsWith("http") ? info.BannerImage : $"/media/{info.BannerImage}") : null,
                    description = info.Description,
                    established_year = info.EstablishedYear,
                    owner_name = info.OwnerName,
                    contact_number = info.ContactNumber,
                    email = info.Email,
                    website = info.Website,
                    opening_time = info.OpeningTime.ToString("HH:mm"),
                    closing_time = info.ClosingTime.ToString("HH:mm"),
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
                    wifi = info.Wifi ?? false,
                    ac = info.Ac ?? false,
                    cctv = info.Cctv ?? false,
                    drinking_water = info.DrinkingWater ?? false,
                    lockers = info.Lockers ?? false,
                    charging_points = info.ChargingPoints ?? false,
                    parking = info.Parking ?? false,
                    reading_area = info.ReadingArea ?? false,
                    computer_access = info.ComputerAccess ?? false,
                    printing = info.Printing ?? false,
                    facebook_url = info.FacebookUrl,
                    instagram_url = info.InstagramUrl,
                    whatsapp_number = info.WhatsappNumber,
                    telegram_url = info.TelegramUrl,
                    youtube_url = info.YoutubeUrl,
                    twitter_url = info.TwitterUrl,
                    linkedin_url = info.LinkedinUrl,
                    tagline = info.Tagline,
                    mission = info.Mission,
                    vision = info.Vision,
                    history = info.History,
                    welcome_message = info.WelcomeMessage,
                    services = info.Services,
                    courses_supported = info.CoursesSupported,
                    statistics_description = info.StatisticsDescription,
                    faq = !string.IsNullOrEmpty(info.Faq) ? System.Text.Json.JsonSerializer.Deserialize<object>(info.Faq) : null,
                    emergency_contact = info.EmergencyContact,
                    footer_text = info.FooterText,
                    testimonials = info.Testimonials != null && info.Testimonials.StartsWith("\"") ? System.Text.Json.JsonSerializer.Deserialize<string>(info.Testimonials) : info.Testimonials,
                    created_at = info.CreatedAt,
                    updated_at = info.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException != null ? ex.InnerException.Message : "";
                return ServiceResult<object>.Fail($"DB Error in GetLibraryInfo: {ex.Message} | {innerMsg}");
            }
        }

        public async Task<ServiceResult<object>> UpdateLibraryInfo(AdminLibraryController.LibraryInfoUpdateDto dto, CancellationToken ct = default)
        {
            try
            {
                var info = await _context.LibraryLibraryinfos.OrderBy(l => l.Id).FirstOrDefaultAsync(ct);
                if (info == null)
                {
                    info = new LibraryLibraryinfo
                    {
                        LibraryName = dto.LibraryName ?? "Shresht Library",
                        Logo = "",
                        Description = dto.Description ?? "Library Description",
                        OwnerName = dto.OwnerName ?? "Admin",
                        ContactNumber = dto.ContactNumber ?? "0000000000",
                        Email = dto.Email ?? "",
                        AddressLine1 = dto.AddressLine1 ?? "Address 1",
                        Area = dto.Area ?? "Area",
                        City = dto.City ?? "City",
                        State = dto.State ?? "State",
                        Country = dto.Country ?? "India",
                        PinCode = dto.PinCode ?? "000000",
                        OpeningTime = new TimeOnly(8, 0),
                        ClosingTime = new TimeOnly(20, 0),
                        CreatedAt = DateTime.UtcNow
                    };
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
                if (dto.TwitterUrl != null) info.TwitterUrl = dto.TwitterUrl;
                if (dto.LinkedinUrl != null) info.LinkedinUrl = dto.LinkedinUrl;

                if (dto.Tagline != null) info.Tagline = dto.Tagline;
                if (dto.Mission != null) info.Mission = dto.Mission;
                if (dto.Vision != null) info.Vision = dto.Vision;
                if (dto.History != null) info.History = dto.History;
                if (dto.WelcomeMessage != null) info.WelcomeMessage = dto.WelcomeMessage;
                if (dto.Services != null) info.Services = dto.Services;
                if (dto.CoursesSupported != null) info.CoursesSupported = dto.CoursesSupported;
                if (dto.StatisticsDescription != null) info.StatisticsDescription = dto.StatisticsDescription;
                if (dto.Faq != null) info.Faq = dto.Faq;
                if (dto.Testimonials != null) info.Testimonials = System.Text.Json.JsonSerializer.Serialize(dto.Testimonials);
                if (dto.EmergencyContact != null) info.EmergencyContact = dto.EmergencyContact;
                if (dto.FooterText != null) info.FooterText = dto.FooterText;


                if (dto.Logo != null) info.Logo = await _cloudinary.UploadImageAsync(dto.Logo, "library") ?? info.Logo;
                if (dto.BannerImage != null) info.BannerImage = await _cloudinary.UploadImageAsync(dto.BannerImage, "library") ?? info.BannerImage;

                info.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(ct);
                return ServiceResult<object>.Ok(new
                {
                    id = info.Id,
                    library_name = info.LibraryName,
                    description = info.Description,
                    tagline = info.Tagline,
                    owner_name = info.OwnerName,
                    contact_number = info.ContactNumber,
                    email = info.Email,
                    website = info.Website,
                    address_line1 = info.AddressLine1,
                    address_line2 = info.AddressLine2,
                    city = info.City,
                    state = info.State,
                    country = info.Country,
                    pin_code = info.PinCode,
                    established_year = info.EstablishedYear,

                    testimonials = info.Testimonials != null && info.Testimonials.StartsWith("\"") ? System.Text.Json.JsonSerializer.Deserialize<string>(info.Testimonials) : info.Testimonials,
                    updated_at = info.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException != null ? ex.InnerException.Message : "";
                return ServiceResult<object>.Fail($"DB Error in UpdateLibraryInfo: {ex.Message} | {innerMsg}");
            }
        }

        public async Task<ServiceResult<object>> GetFacilities(CancellationToken ct = default)
        {
            var facilities = await _context.LibraryFacilities.AsNoTracking().OrderBy(f => f.Order).ToListAsync(ct);
            var data = facilities.Select(f => new {
                id = f.Id,
                name = f.Name,
                description = f.Description,
                image = !string.IsNullOrEmpty(f.Image) ? (f.Image.StartsWith("http") ? f.Image : $"/media/{f.Image}") : null,
                icon_key = f.IconKey,
                order = f.Order,
                is_active = f.IsActive
            });
            return ServiceResult<object>.Ok(data);
        }

        public async Task<ServiceResult<object>> CreateFacility(AdminLibraryController.FacilityDto dto, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(dto.Name)) return ServiceResult<object>.Fail("Name is required");
            var iconPath = await _cloudinary.UploadImageAsync(dto.Image, "library");
            int order = 0;
            if (!string.IsNullOrWhiteSpace(dto.Order) && int.TryParse(dto.Order, out var o)) order = o;

            bool isActive = true;
            if (!string.IsNullOrWhiteSpace(dto.IsActive) && bool.TryParse(dto.IsActive, out var a)) isActive = a;

            var facility = new LibraryFacility
            {
                Name = dto.Name,
                Description = dto.Description ?? "",
                Image = iconPath ?? "",
                IconKey = dto.IconKey ?? "default",
                Order = order,
                IsActive = isActive
            };
            _context.LibraryFacilities.Add(facility);
            await _context.SaveChangesAsync(ct);
            return ServiceResult<object>.Ok(new
            {
                id = facility.Id,
                name = facility.Name,
                description = facility.Description,
                image = !string.IsNullOrEmpty(facility.Image) ? (facility.Image.StartsWith("http") ? facility.Image : $"/media/{facility.Image}") : null,
                icon_key = facility.IconKey,
                order = facility.Order,
                is_active = facility.IsActive
            });
        }

        public async Task<ServiceResult<object>> UpdateFacility(long id, AdminLibraryController.FacilityDto dto, CancellationToken ct = default)
        {
            var facility = await _context.LibraryFacilities.FindAsync(new object[] { id }, ct);
            if (facility == null) return ServiceResult<object>.NotFound("Not found");

            if (dto.Name != null) facility.Name = dto.Name;
            if (dto.Description != null) facility.Description = dto.Description;
            if (dto.IconKey != null) facility.IconKey = dto.IconKey;
            if (!string.IsNullOrWhiteSpace(dto.Order) && int.TryParse(dto.Order, out var o)) facility.Order = o;
            if (!string.IsNullOrWhiteSpace(dto.IsActive) && bool.TryParse(dto.IsActive, out var a)) facility.IsActive = a;
            if (dto.Image != null)
            {
                facility.Image = await _cloudinary.UploadImageAsync(dto.Image, "library") ?? facility.Image;
            }

            await _context.SaveChangesAsync(ct);
            return ServiceResult<object>.Ok(new
            {
                id = facility.Id,
                name = facility.Name,
                description = facility.Description,
                image = !string.IsNullOrEmpty(facility.Image) ? (facility.Image.StartsWith("http") ? facility.Image : $"/media/{facility.Image}") : null,
                icon_key = facility.IconKey,
                order = facility.Order,
                is_active = facility.IsActive
            });
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
                photo = !string.IsNullOrEmpty(a.Photo) ? (a.Photo.StartsWith("http") ? a.Photo : $"/media/{a.Photo}") : null,
                order = a.Order,
                is_active = a.IsActive
            });
            return ServiceResult<object>.Ok(data);
        }

        public async Task<ServiceResult<object>> CreateAchiever(AdminLibraryController.AchieverDto dto, CancellationToken ct = default)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Name)) return ServiceResult<object>.Fail("Name is required");
                var imagePath = dto.Photo != null ? await _cloudinary.UploadImageAsync(dto.Photo, "library") : null;
                int year = DateTime.Now.Year;
                if (!string.IsNullOrWhiteSpace(dto.Year) && int.TryParse(dto.Year, out var y)) year = y;

                bool isFeatured = false;
                if (!string.IsNullOrWhiteSpace(dto.IsFeatured) && bool.TryParse(dto.IsFeatured, out var f)) isFeatured = f;

                int order = 0;
                if (!string.IsNullOrWhiteSpace(dto.Order) && int.TryParse(dto.Order, out var o)) order = o;

                bool isActive = true;
                if (!string.IsNullOrWhiteSpace(dto.IsActive) && bool.TryParse(dto.IsActive, out var a)) isActive = a;

                var achiever = new LibraryAchiever
                {
                    Name = dto.Name,
                    Achievement = dto.Achievement ?? "",
                    Goal = dto.Goal ?? "",
                    Year = year,
                    IsFeatured = isFeatured,
                    Photo = imagePath ?? "",
                    Order = order,
                    IsActive = isActive
                };
                _context.LibraryAchievers.Add(achiever);
                await _context.SaveChangesAsync(ct);
                return ServiceResult<object>.Ok(new
                {
                    id = achiever.Id,
                    name = achiever.Name,
                    achievement = achiever.Achievement,
                    goal = achiever.Goal,
                    year = achiever.Year,
                    is_featured = achiever.IsFeatured,
                    photo = !string.IsNullOrEmpty(achiever.Photo) ? (achiever.Photo.StartsWith("http") ? achiever.Photo : $"/media/{achiever.Photo}") : null,
                    order = achiever.Order,
                    is_active = achiever.IsActive
                });
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException != null ? $"{ex.Message} ({ex.InnerException.Message})" : ex.Message;
                return ServiceResult<object>.Fail($"Error creating achiever: {msg}");
            }
        }

        public async Task<ServiceResult<object>> UpdateAchiever(long id, AdminLibraryController.AchieverDto dto, CancellationToken ct = default)
        {
            try
            {
                var achiever = await _context.LibraryAchievers.FindAsync(new object[] { id }, ct);
                if (achiever == null) return ServiceResult<object>.NotFound("Achiever not found");

                if (dto != null)
                {
                    if (dto.Name != null) achiever.Name = dto.Name;
                    if (dto.Achievement != null) achiever.Achievement = dto.Achievement;
                    if (dto.Goal != null) achiever.Goal = dto.Goal;
                    if (!string.IsNullOrWhiteSpace(dto.Year) && int.TryParse(dto.Year, out var y)) achiever.Year = y;
                    if (!string.IsNullOrWhiteSpace(dto.IsFeatured) && bool.TryParse(dto.IsFeatured, out var f)) achiever.IsFeatured = f;
                    if (!string.IsNullOrWhiteSpace(dto.Order) && int.TryParse(dto.Order, out var o)) achiever.Order = o;
                    if (!string.IsNullOrWhiteSpace(dto.IsActive) && bool.TryParse(dto.IsActive, out var a)) achiever.IsActive = a;
                    if (dto.Photo != null)
                    {
                        var uploaded = await _cloudinary.UploadImageAsync(dto.Photo, "library");
                        if (!string.IsNullOrEmpty(uploaded))
                        {
                            achiever.Photo = uploaded;
                        }
                    }
                }

                await _context.SaveChangesAsync(ct);
                return ServiceResult<object>.Ok(new
                {
                    id = achiever.Id,
                    name = achiever.Name,
                    achievement = achiever.Achievement,
                    goal = achiever.Goal,
                    year = achiever.Year,
                    is_featured = achiever.IsFeatured,
                    photo = !string.IsNullOrEmpty(achiever.Photo) ? (achiever.Photo.StartsWith("http") ? achiever.Photo : $"/media/{achiever.Photo}") : null,
                    order = achiever.Order,
                    is_active = achiever.IsActive
                });
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException != null ? $"{ex.Message} ({ex.InnerException.Message})" : ex.Message;
                return ServiceResult<object>.Fail($"Error updating achiever: {msg}");
            }
        }

        public async Task<ServiceResult<object>> ToggleAchiever(long id, CancellationToken ct = default)
        {
            try
            {
                var achiever = await _context.LibraryAchievers.FindAsync(new object[] { id }, ct);
                if (achiever == null) return ServiceResult<object>.NotFound("Achiever not found");
                achiever.IsActive = !achiever.IsActive;
                await _context.SaveChangesAsync(ct);
                return ServiceResult<object>.Ok("Achiever toggled.");
            }
            catch (Exception ex)
            {
                return ServiceResult<object>.Fail($"Error toggling achiever: {ex.Message}");
            }
        }

        public async Task<ServiceResult<object>> DeleteAchiever(long id, CancellationToken ct = default)
        {
            try
            {
                var achiever = await _context.LibraryAchievers.FindAsync(new object[] { id }, ct);
                if (achiever == null) return ServiceResult<object>.NotFound("Achiever not found");
                _context.LibraryAchievers.Remove(achiever);
                await _context.SaveChangesAsync(ct);
                return ServiceResult<object>.Ok("Achiever deleted.");
            }
            catch (Exception ex)
            {
                return ServiceResult<object>.Fail($"Error deleting achiever: {ex.Message}");
            }
        }

        public async Task<ServiceResult<object>> GetReviews(int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            var totalCount = await _context.LibraryReviews.CountAsync(ct);
            var totalPages = pageSize > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 1;
            var reviews = await _context.LibraryReviews.AsNoTracking().Include(r => r.Student)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);
            var data = reviews.Select(r => new {
                id = r.Id,
                student = r.StudentId,
                student_name = r.Student != null ? r.Student.FirstName + " " + r.Student.LastName : "Unknown",
                rating = r.Rating,
                comment = r.Comment,
                is_approved = r.IsApproved,
                rejection_reason = r.RejectionReason,
                created_at = r.CreatedAt,
                updated_at = (DateTime?)null
            });
            return ServiceResult<object>.Ok(new
            {
                count = totalCount,
                total_pages = totalPages,
                current_page = page,
                next = (string?)null,
                previous = (string?)null,
                data = data
            });
        }

        public async Task<ServiceResult<object>> GetPendingReviews(int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            var query = _context.LibraryReviews.AsNoTracking().Include(r => r.Student).Where(r => r.IsApproved == false && r.RejectionReason == null);
            var totalCount = await query.CountAsync(ct);
            var totalPages = pageSize > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 1;
            var reviews = await query.OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);
            var data = reviews.Select(r => new {
                id = r.Id,
                student = r.StudentId,
                student_name = r.Student != null ? r.Student.FirstName + " " + r.Student.LastName : "Unknown",
                rating = r.Rating,
                comment = r.Comment,
                is_approved = r.IsApproved,
                rejection_reason = r.RejectionReason,
                created_at = r.CreatedAt,
                updated_at = (DateTime?)null
            });
            return ServiceResult<object>.Ok(new
            {
                count = totalCount,
                total_pages = totalPages,
                current_page = page,
                next = (string?)null,
                previous = (string?)null,
                data = data
            });
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
            var approvedReviews = await _context.LibraryReviews.AsNoTracking()
                .Where(r => r.IsApproved == true)
                .ToListAsync();
            var count = approvedReviews.Count;
            var averageRating = count > 0 ? approvedReviews.Average(r => r.Rating) : 0.0;
            var breakdown = approvedReviews
                .GroupBy(r => r.Rating)
                .ToDictionary(g => g.Key, g => g.Count());
            return ServiceResult<object>.Ok(new { count, average_rating = Math.Round(averageRating, 1), breakdown });
        }

        public async Task<ServiceResult<object>> GetGalleryImages(CancellationToken ct = default)
        {
            var images = await _context.LibraryGalleryImages.AsNoTracking().OrderBy(i => i.Order).ThenByDescending(i => i.CreatedAt).ToListAsync(ct);
            var data = images.Select(i => new {
                id = i.Id,
                image_url = !string.IsNullOrEmpty(i.ImageUrl) ? (i.ImageUrl.StartsWith("http") ? i.ImageUrl : $"/media/{i.ImageUrl}") : null,
                caption = i.Caption,
                order = i.Order,
                created_at = i.CreatedAt
            });
            return ServiceResult<object>.Ok(data);
        }

        public async Task<ServiceResult<object>> UploadGalleryImage(AdminLibraryController.GalleryImageDto dto, CancellationToken ct = default)
        {
            var imagePath = await _cloudinary.UploadImageAsync(dto.Image, "library");
            if (string.IsNullOrEmpty(imagePath)) return ServiceResult<object>.Fail("Failed to upload image.");

            int order = 0;
            if (!string.IsNullOrWhiteSpace(dto.Order) && int.TryParse(dto.Order, out var o)) order = o;

            var galleryImage = new LibraryGalleryImage
            {
                ImageUrl = imagePath,
                Caption = dto.Caption,
                Order = order,
                CreatedAt = DateTime.UtcNow
            };
            _context.LibraryGalleryImages.Add(galleryImage);
            await _context.SaveChangesAsync(ct);

            return ServiceResult<object>.Ok(new
            {
                id = galleryImage.Id,
                image_url = !string.IsNullOrEmpty(galleryImage.ImageUrl) ? (galleryImage.ImageUrl.StartsWith("http") ? galleryImage.ImageUrl : $"/media/{galleryImage.ImageUrl}") : null,
                caption = galleryImage.Caption,
                order = galleryImage.Order,
                created_at = galleryImage.CreatedAt
            });
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
