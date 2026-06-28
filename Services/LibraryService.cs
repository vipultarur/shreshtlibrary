using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WebApplication1.Controllers;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public class LibraryService : ILibraryService
    {
        private readonly ApplicationDbContext _context;
        private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _cache;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        public LibraryService(ApplicationDbContext context, Microsoft.Extensions.Caching.Memory.IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<ServiceResult<object>> GetLibraryInfoAsync(CancellationToken ct = default)
        {
            const string cacheKey = "LibraryInfo";
            if (_cache.TryGetValue(cacheKey, out object? cachedInfo))
            {
                return ServiceResult<object>.Ok(cachedInfo);
            }

            var info = await _context.LibraryLibraryinfos.AsNoTracking().FirstOrDefaultAsync(ct);
            if (info == null) return ServiceResult<object>.Ok(null);

            var responseData = new
            {
                id = info.Id,
                name = info.Name,
                description = info.Description,
                address = info.Address,
                contact_email = info.Email,
                contact_phone = info.PhonePrimary,
                opening_time = info.OpenTime?.ToString("HH:mm:ss"),
                closing_time = info.CloseTime?.ToString("HH:mm:ss"),
                total_seats = 0
            };
            
            _cache.Set(cacheKey, responseData, CacheDuration);
            return ServiceResult<object>.Ok(responseData);
        }

        public async Task<ServiceResult<object>> GetFacilitiesAsync(CancellationToken ct = default)
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
                    title = f.Name,
                    description = f.Description,
                    icon_name = f.IconKey,
                    is_active = f.IsActive
                })
                .ToListAsync(ct);

            _cache.Set(cacheKey, facilities, CacheDuration);
            return ServiceResult<object>.Ok(facilities);
        }

        public async Task<ServiceResult<object>> GetAchieversAsync(bool? featured, string mediaBaseUrl, CancellationToken ct = default)
        {
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
                    student_name = a.Name,
                    achievement_title = a.Achievement,
                    description = a.Goal,
                    image = !string.IsNullOrEmpty(a.Photo) ? $"{mediaBaseUrl}/media/{a.Photo}" : null,
                    achievement_date = a.Year.ToString()
                })
                .ToListAsync(ct);

            return ServiceResult<object>.Ok(achievers);
        }

        public async Task<ServiceResult<object>> GetReviewsAsync(CancellationToken ct = default)
        {
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

            return ServiceResult<object>.Ok(reviews);
        }

        public async Task<ServiceResult<object>> GetReviewsSummaryAsync(CancellationToken ct = default)
        {
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
            return ServiceResult<object>.Ok(summaryData);
        }

        public async Task<ServiceResult<object>> SubmitReviewAsync(long userId, LibraryController.SubmitReviewRequest request, CancellationToken ct = default)
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
            var sliders = await _context.LibraryHomesliders
                .AsNoTracking()
                .Where(s => s.IsActive)
                .OrderBy(s => s.SortOrder)
                .Select(s => new
                {
                    id = s.Id,
                    title = s.Title,
                    description = s.Subtitle,
                    image = !string.IsNullOrEmpty(s.Image) ? $"{mediaBaseUrl}/media/{s.Image}" : null,
                    link = s.LinkUrl,
                    order = s.SortOrder,
                    is_active = s.IsActive
                })
                .ToListAsync(ct);

            return ServiceResult<object>.Ok(sliders);
        }
    }
}
