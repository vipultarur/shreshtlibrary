using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WebApplication1.Controllers;
using WebApplication1.Models.DTOs.Library;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public class LibraryService : ILibraryService
    {
        private readonly ApplicationDbContext _context;
        private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _cache;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

        public LibraryService(ApplicationDbContext context, Microsoft.Extensions.Caching.Memory.IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<ServiceResult<object>> GetLibraryInfoAsync(string mediaBaseUrl, CancellationToken ct = default)
        {
            const string cacheKey = "LibraryInfo";
            if (_cache.TryGetValue(cacheKey, out object? cachedInfo))
            {
                return ServiceResult<object>.Ok(cachedInfo);
            }

            var info = await _context.LibraryLibraryinfos.AsNoTracking().OrderBy(i => i.Id).FirstOrDefaultAsync(ct);
            if (info == null) return ServiceResult<object>.Ok(null);

            var appConfig = await _context.LibraryAppconfigs.AsNoTracking().OrderBy(i => i.Id).FirstOrDefaultAsync(ct);

            var maintenanceSetting = await _context.CoreGlobalsettings.AsNoTracking().FirstOrDefaultAsync(s => s.Key == "MAINTENANCE_MODE", ct);
            bool maintenanceMode = maintenanceSetting?.Value == "true";

            var responseData = new
            {
                library_name = info.LibraryName,
                logo = !string.IsNullOrEmpty(info.Logo) ? (info.Logo.StartsWith("http") ? info.Logo : $"{mediaBaseUrl}/media/{info.Logo}") : null,
                banner_image = !string.IsNullOrEmpty(info.BannerImage) ? (info.BannerImage.StartsWith("http") ? info.BannerImage : $"{mediaBaseUrl}/media/{info.BannerImage}") : null,
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
                faq = info.Faq,
                testimonials = info.Testimonials,
                emergency_contact = info.EmergencyContact,
                footer_text = info.FooterText,

                created_at = info.CreatedAt,
                updated_at = info.UpdatedAt,
                enable_whatsapp_service = appConfig?.EnableWhatsappService ?? false,
                maintenance_mode = maintenanceMode
            };
            
            _cache.Set(cacheKey, responseData, CacheDuration);
            return ServiceResult<object>.Ok(responseData);
        }

        public async Task<ServiceResult<object>> GetFacilitiesAsync(string mediaBaseUrl, CancellationToken ct = default)
        {
            const string cacheKey = "LibraryFacilities";
            if (_cache.TryGetValue(cacheKey, out object? cachedFacilities))
            {
                return ServiceResult<object>.Ok(cachedFacilities);
            }

            var facilities = await _context.LibraryFacilities
                .AsNoTracking()
                .Where(f => f.IsActive)
                .OrderBy(f => f.Order)
                .Select(f => new
                {
                    id = f.Id,
                    name = f.Name,
                    description = f.Description,
                    icon_key = f.IconKey,
                    image = !string.IsNullOrEmpty(f.Image) ? (f.Image.StartsWith("http") ? f.Image : $"{mediaBaseUrl}/media/{f.Image}") : null
                })
                .ToListAsync(ct);

            _cache.Set(cacheKey, facilities, CacheDuration);
            return ServiceResult<object>.Ok(facilities);
        }

        public async Task<ServiceResult<object>> GetAchieversAsync(bool? featured, string mediaBaseUrl, CancellationToken ct = default)
        {
            string cacheKey = $"LibraryAchievers_{(featured.HasValue && featured.Value ? "Featured" : "All")}";
            if (_cache.TryGetValue(cacheKey, out object? cachedAchievers))
            {
                return ServiceResult<object>.Ok(cachedAchievers);
            }

            var query = _context.LibraryAchievers.AsNoTracking().Where(a => a.IsActive);
            if (featured.HasValue && featured.Value)
            {
                query = query.Where(a => a.IsFeatured);
            }

            var achievers = await query
                .OrderByDescending(a => a.Order)
                .Select(a => new
                {
                    id = a.Id,
                    name = a.Name,
                    achievement = a.Achievement,
                    goal = a.Goal,
                    photo = !string.IsNullOrEmpty(a.Photo) ? (a.Photo.StartsWith("http") ? a.Photo : $"{mediaBaseUrl}/media/{a.Photo}") : null,
                    year = a.Year
                })
                .ToListAsync(ct);

            _cache.Set(cacheKey, achievers, CacheDuration);
            return ServiceResult<object>.Ok(achievers);
        }

        public async Task<ServiceResult<object>> GetReviewsAsync(CancellationToken ct = default)
        {
            const string cacheKey = "LibraryReviews";
            if (_cache.TryGetValue(cacheKey, out object? cachedReviews))
            {
                return ServiceResult<object>.Ok(cachedReviews);
            }

            var reviews = await _context.LibraryReviews
                .AsNoTracking()
                .Include(r => r.Student)
                .Where(r => r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    id = r.Id,
                    student = new
                    {
                        id = r.Student.Id,
                        username = r.Student.Username,
                        first_name = r.Student.FirstName,
                        last_name = r.Student.LastName
                    },
                    rating = r.Rating,
                    comment = r.Comment,
                    created_at = r.CreatedAt.HasValue ? r.CreatedAt.Value.ToString("O") : null,
                    is_approved = r.IsApproved
                })
                .ToListAsync(ct);

            _cache.Set(cacheKey, reviews, CacheDuration);
            return ServiceResult<object>.Ok(reviews);
        }

        public async Task<ServiceResult<object>> GetReviewsSummaryAsync(CancellationToken ct = default)
        {
            const string cacheKey = "LibraryReviewsSummary";
            if (_cache.TryGetValue(cacheKey, out object? cachedSummary))
            {
                return ServiceResult<object>.Ok(cachedSummary);
            }

            var stats = await _context.LibraryReviews
                .AsNoTracking()
                .Where(r => r.IsApproved)
                .GroupBy(r => 1)
                .Select(g => new {
                    Total = g.Count(),
                    Average = g.Average(r => (double)r.Rating),
                    R1 = g.Count(r => r.Rating == 1),
                    R2 = g.Count(r => r.Rating == 2),
                    R3 = g.Count(r => r.Rating == 3),
                    R4 = g.Count(r => r.Rating == 4),
                    R5 = g.Count(r => r.Rating == 5)
                })
                .FirstOrDefaultAsync(ct);

            var summaryData = new
            {
                total_reviews = stats?.Total ?? 0,
                average_rating = stats != null ? Math.Round(stats.Average, 1) : 0,
                rating_counts = new
                {
                    _1 = stats?.R1 ?? 0,
                    _2 = stats?.R2 ?? 0,
                    _3 = stats?.R3 ?? 0,
                    _4 = stats?.R4 ?? 0,
                    _5 = stats?.R5 ?? 0
                }
            };
            
            _cache.Set(cacheKey, summaryData, CacheDuration);
            return ServiceResult<object>.Ok(summaryData);
        }

        public async Task<ServiceResult<object>> GetMyReviewAsync(long userId, CancellationToken ct = default)
        {
            var review = await _context.LibraryReviews
                .AsNoTracking()
                .Where(r => r.StudentId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    id = r.Id,
                    rating = r.Rating,
                    comment = r.Comment,
                    created_at = r.CreatedAt.HasValue ? r.CreatedAt.Value.ToString("O") : null,
                    is_approved = r.IsApproved
                })
                .FirstOrDefaultAsync(ct);

            // Return null if not found, to let frontend know the user hasn't reviewed yet
            return ServiceResult<object>.Ok(review);
        }

        public async Task<ServiceResult<object>> SubmitReviewAsync(long userId, SubmitReviewRequest request, CancellationToken ct = default)
        {
            if (request.rating < 1 || request.rating > 5)
                return ServiceResult<object>.Fail("Rating must be between 1 and 5");

            var review = new LibraryReview
            {
                StudentId = userId,
                Rating = request.rating,
                Comment = request.comment ?? "",
                IsApproved = false, // Needs admin approval
                CreatedAt = DateTime.UtcNow
            };

            _context.LibraryReviews.Add(review);
            await _context.SaveChangesAsync(ct);

            return ServiceResult<object>.Ok(new
            {
                id = review.Id,
                rating = review.Rating,
                comment = review.Comment,
                created_at = review.CreatedAt.HasValue ? review.CreatedAt.Value.ToString("O") : null,
                is_approved = review.IsApproved
            });
        }

        public async Task<ServiceResult<object>> GetSlidersAsync(string mediaBaseUrl, CancellationToken ct = default)
        {
            const string cacheKey = "LibrarySlidersStudent";
            if (_cache.TryGetValue(cacheKey, out object? cachedSliders))
            {
                return ServiceResult<object>.Ok(cachedSliders);
            }

            var sliders = await _context.LibraryHomesliders
                .AsNoTracking()
                .Where(s => s.IsActive)
                .OrderBy(s => s.SortOrder)
                .Select(s => new
                {
                    id = s.Id,
                    title = s.Title,
                    description = s.Subtitle,
                    image = !string.IsNullOrEmpty(s.Image) ? (s.Image.StartsWith("http") ? s.Image : $"{mediaBaseUrl}/media/{s.Image}") : null,
                    link = s.LinkUrl,
                    order = s.SortOrder,
                    is_active = s.IsActive
                })
                .ToListAsync(ct);

            _cache.Set(cacheKey, sliders, CacheDuration);
            return ServiceResult<object>.Ok(sliders);
        }

        public async Task<ServiceResult<object>> GetGalleryImagesAsync(string mediaBaseUrl, CancellationToken ct = default)
        {
            const string cacheKey = "LibraryGalleryImages";
            if (_cache.TryGetValue(cacheKey, out object? cachedGallery))
            {
                return ServiceResult<object>.Ok(cachedGallery);
            }

            var images = await _context.LibraryGalleryImages
                .AsNoTracking()
                .OrderBy(i => i.Order)
                .ThenByDescending(i => i.CreatedAt)
                .Select(i => new
                {
                    id = i.Id,
                    image_url = !string.IsNullOrEmpty(i.ImageUrl) ? (i.ImageUrl.StartsWith("http") ? i.ImageUrl : $"{mediaBaseUrl}/media/{i.ImageUrl}") : null,
                    caption = i.Caption,
                    order = i.Order,
                    created_at = i.CreatedAt
                })
                .ToListAsync(ct);

            _cache.Set(cacheKey, images, CacheDuration);
            return ServiceResult<object>.Ok(images);
        }
    }
}

