using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Controllers;
using WebApplication1.Data;
using WebApplication1.Models;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using WebApplication1.Models.DTOs.Study;

namespace WebApplication1.Services
{
    public class StudyService : IStudyService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public StudyService(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        private object FormatSession(WebApplication1.Models.StudyStudysession session)
        {
            return new
            {
                id = session.Id,
                start_time = session.StartTime.ToString("O"),
                end_time = session.EndTime?.ToString("O"),
                duration_minutes = session.DurationMinutes,
                paused_minutes = session.PausedMinutes,
                status = session.Status,
                student = session.StudentId
            };
        }

        public async Task<ServiceResult<object>> StartSessionAsync(long userId, CancellationToken ct = default)
        {
            var activeSessions = await _context.StudyStudysessions
                .Where(s => s.StudentId == userId && s.Status != "completed" && s.Status != "ended")
                .ToListAsync(ct);

            foreach (var s in activeSessions)
            {
                s.Status = "completed";
                s.EndTime = DateTime.UtcNow;
            }

            var session = new WebApplication1.Models.StudyStudysession
            {
                StudentId = userId,
                StartTime = DateTime.UtcNow,
                Status = "active",
                DurationMinutes = 0,
                PausedMinutes = 0
            };

            _context.StudyStudysessions.Add(session);
            await _context.SaveChangesAsync(ct);
            _cache.Remove($"SessionHistory_{userId}");

            return ServiceResult<object>.Ok(FormatSession(session));
        }

        public async Task<ServiceResult<object>> EndSessionAsync(long userId, EndSessionRequest request, CancellationToken ct = default)
        {
            var session = await _context.StudyStudysessions
                .Where(s => s.StudentId == userId && s.Status != "completed" && s.Status != "ended")
                .OrderByDescending(s => s.StartTime)
                .FirstOrDefaultAsync(ct);

            if (session == null)
            {
                var latestSession = await _context.StudyStudysessions
                    .AsNoTracking()
                    .Where(s => s.StudentId == userId)
                    .OrderByDescending(s => s.StartTime)
                    .FirstOrDefaultAsync(ct);

                if (latestSession != null)
                {
                    return ServiceResult<object>.Ok(FormatSession(latestSession));
                }
                
                return ServiceResult<object>.Fail("No active session found.");
            }

            session.Status = "completed";
            session.EndTime = DateTime.UtcNow;
            if (request != null)
            {
                session.DurationMinutes = request.duration_minutes;
                session.PausedMinutes = request.paused_minutes;
            }

            await _context.SaveChangesAsync(ct);
            _cache.Remove($"SessionHistory_{userId}");
            return ServiceResult<object>.Ok(FormatSession(session));
        }

        public async Task<ServiceResult<object>> GetCurrentSessionAsync(long userId, CancellationToken ct = default)
        {
            var session = await _context.StudyStudysessions
                .AsNoTracking()
                .Where(s => s.StudentId == userId && s.Status != "completed" && s.Status != "ended")
                .OrderByDescending(s => s.StartTime)
                .FirstOrDefaultAsync(ct);

            if (session == null)
            {
                return ServiceResult<object>.Ok(null);
            }

            return ServiceResult<object>.Ok(FormatSession(session));
        }

        public async Task<ServiceResult<object>> UpdateSessionAsync(long userId, UpdateSessionRequest request, CancellationToken ct = default)
        {
            var session = await _context.StudyStudysessions
                .Where(s => s.StudentId == userId && s.Status != "completed" && s.Status != "ended")
                .OrderByDescending(s => s.StartTime)
                .FirstOrDefaultAsync(ct);

            if (session == null)
            {
                return ServiceResult<object>.Fail("No active session found.");
            }

            if (!string.IsNullOrEmpty(request?.status))
            {
                session.Status = request.status;
            }
            if (request?.duration_minutes != null)
            {
                session.DurationMinutes = request.duration_minutes.Value;
            }
            if (request?.paused_minutes != null)
            {
                session.PausedMinutes = request.paused_minutes.Value;
            }

            await _context.SaveChangesAsync(ct);
            _cache.Remove($"SessionHistory_{userId}");
            return ServiceResult<object>.Ok(FormatSession(session));
        }

        public async Task<ServiceResult<object>> GetSessionHistoryAsync(long userId, int page, int pageSize, CancellationToken ct = default)
        {
            string cacheKey = $"SessionHistory_{userId}";
            if (!_cache.TryGetValue(cacheKey, out System.Collections.Generic.List<object>? allSessions) || allSessions == null)
            {
                var sessions = await _context.StudyStudysessions
                    .AsNoTracking()
                    .Where(s => s.StudentId == userId)
                    .OrderByDescending(s => s.StartTime)
                    .ToListAsync(ct);

                allSessions = sessions.Select(FormatSession).ToList();
                _cache.Set(cacheKey, allSessions, TimeSpan.FromMinutes(15));
            }

            var pagedData = allSessions
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return ServiceResult<object>.Ok(pagedData);
        }

        public async Task<ServiceResult<object>> GetLeaderboardAsync(string duration = "month", string? startDate = null, string? endDate = null, string mediaBaseUrl = "", CancellationToken ct = default)
        {
            string cacheKey = $"StudyLeaderboard_{duration}_{startDate}_{endDate}";
            if (_cache.TryGetValue(cacheKey, out object? cachedLeaderboard) && cachedLeaderboard != null)
            {
                return ServiceResult<object>.Ok(cachedLeaderboard);
            }

            var nowUtc = DateTime.UtcNow;
            
            var query = _context.StudyStudysessions.AsNoTracking().Where(s => s.Status == "completed");
            
            if (duration == "today")
            {
                var todayStart = nowUtc.Date;
                var todayEnd = todayStart.AddDays(1);
                query = query.Where(s => s.StartTime >= todayStart && s.StartTime < todayEnd);
            }
            else if (duration == "week")
            {
                var diff = nowUtc.DayOfWeek - DayOfWeek.Monday;
                if (diff < 0) diff += 7;
                var weekStart = nowUtc.Date.AddDays(-diff);
                query = query.Where(s => s.StartTime >= weekStart);
            }
            else if (duration == "year")
            {
                var yearStart = new DateTime(nowUtc.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                query = query.Where(s => s.StartTime >= yearStart);
            }
            else if (duration == "custom" && !string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
            {
                if (DateTime.TryParse(startDate, out var sd) && DateTime.TryParse(endDate, out var ed))
                {
                    sd = DateTime.SpecifyKind(sd, DateTimeKind.Utc);
                    ed = DateTime.SpecifyKind(ed.Date.AddDays(1), DateTimeKind.Utc);
                    query = query.Where(s => s.StartTime >= sd && s.StartTime < ed);
                }
            }
            else // month
            {
                var monthStart = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                query = query.Where(s => s.StartTime >= monthStart);
            }

            var leaderboardData = await query
                .GroupBy(s => s.StudentId)
                .Select(g => new {
                    StudentId = g.Key,
                    TotalMinutes = g.Sum(s => s.DurationMinutes)
                })
                .Join(_context.StudentsStudentprofiles,
                    s => s.StudentId,
                    p => p.UserId,
                    (s, p) => new { s.StudentId, s.TotalMinutes, Profile = p })
                .Join(_context.AccountsCustomusers,
                    j => j.StudentId,
                    u => u.Id,
                    (j, u) => new { j.StudentId, j.TotalMinutes, Profile = j.Profile, User = u })
                .Where(j => j.Profile.Status != "EXPIRED" && j.Profile.Status != "SUSPENDED")
                .OrderByDescending(x => x.TotalMinutes)
                .Take(100)
                .Select(x => new {
                    x.StudentId,
                    x.TotalMinutes,
                    ProfileId = x.Profile.Id,
                    ProfileStudentId = x.Profile.StudentId,
                    ProfilePhoto = x.Profile.ProfilePhoto,
                    Username = x.User.Username,
                    FirstName = x.User.FirstName,
                    LastName = x.User.LastName
                })
                .ToListAsync(ct);

            var leaderboard = new System.Collections.Generic.List<object>();
            int rank = 1;

            foreach (var item in leaderboardData)
            {
                double hours = item.TotalMinutes / 60.0;
                int lvl = 1; string t = "Newbie"; string c = "#94a3b8"; // Gray

                if (hours >= 280) { lvl = 9; t = "Library Legend"; c = "#22d3ee"; } // Cyan
                else if (hours >= 220) { lvl = 8; t = "Grand Master Scholar"; c = "#fbbf24"; } // Gold
                else if (hours >= 170) { lvl = 7; t = "Academic Champion"; c = "#c084fc"; } // Purple
                else if (hours >= 130) { lvl = 6; t = "Book Slayer"; c = "#f87171"; } // Red
                else if (hours >= 90) { lvl = 5; t = "Study Warrior"; c = "#fb923c"; } // Orange
                else if (hours >= 60) { lvl = 4; t = "Knowledge Knight"; c = "#BDAD40"; } // Light Blue
                else if (hours >= 30) { lvl = 3; t = "Library Squire"; c = "#4ade80"; } // Green
                else if (hours >= 10) { lvl = 2; t = "Rookie Scholar"; c = "#69A3E0"; } // Blue

                leaderboard.Add(new
                {
                    rank = rank,
                    student = new
                    {
                        id = item.ProfileId,
                        user_id = item.StudentId,
                        student_id = item.ProfileStudentId,
                        username = item.Username,
                        first_name = item.FirstName,
                        last_name = item.LastName,
                        profile_image = !string.IsNullOrEmpty(item.ProfilePhoto) ? (item.ProfilePhoto.StartsWith("http") ? item.ProfilePhoto : $"{mediaBaseUrl}/media/{item.ProfilePhoto}") : null
                    },
                    total_minutes = item.TotalMinutes,
                    hours_formatted = $"{item.TotalMinutes / 60}h {item.TotalMinutes % 60}m",
                    level_info = new { level = lvl, title = t, badge_color = c }
                });
                rank++;
            }

            _cache.Set(cacheKey, leaderboard, TimeSpan.FromMinutes(10));
            return ServiceResult<object>.Ok(leaderboard);
        }
    }
}
