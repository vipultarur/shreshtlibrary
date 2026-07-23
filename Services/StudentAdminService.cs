using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using WebApplication1.Controllers;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public class StudentAdminService : IStudentAdminService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IMemoryCache _cache;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ICloudinaryService _cloudinary;
        private readonly Microsoft.Extensions.Logging.ILogger<StudentAdminService> _logger;

        public StudentAdminService(ApplicationDbContext context, IEmailService emailService, IMemoryCache cache, IDateTimeProvider dateTimeProvider, IServiceScopeFactory scopeFactory, ICloudinaryService cloudinary, Microsoft.Extensions.Logging.ILogger<StudentAdminService> logger)
        {
            _context = context;
            _emailService = emailService;
            _cache = cache;
            _dateTimeProvider = dateTimeProvider;
            _scopeFactory = scopeFactory;
            _cloudinary = cloudinary;
            _logger = logger;
        }

        public async Task<ServiceResult<object>> GetStudentCountsAsync(CancellationToken ct = default)
        {
            if (_cache.TryGetValue("StudentCounts", out var cachedResult))
            {
                return ServiceResult<object>.Ok(cachedResult!);
            }

            var total = await _context.AccountsCustomusers.CountAsync(u => u.Role == WebApplication1.Utils.Constants.Roles.Student && !u.IsDeleted, ct);
            
            var stats = await _context.StudentsStudentprofiles
                .Where(x => !x.IsDeleted)
                .GroupBy(x => 1)
                .Select(g => new {
                    Live = g.Count(x => x.Status == WebApplication1.Utils.Constants.StudentStatus.Live),
                    Expired = g.Count(x => x.Status == WebApplication1.Utils.Constants.StudentStatus.Expired),
                    Suspended = g.Count(x => x.Status == WebApplication1.Utils.Constants.StudentStatus.Suspended),
                    Pending = g.Count(x => x.Status == WebApplication1.Utils.Constants.StudentStatus.Pending),
                    Girls = g.Count(x => x.Gender == "F" || x.Gender == "Female"),
                    Boys = g.Count(x => x.Gender == "M" || x.Gender == "Male"),
                    Other = g.Count(x => x.Gender == "O" || x.Gender == "Other")
                })
                .FirstOrDefaultAsync(ct);

            var live = stats?.Live ?? 0;
            var expired = stats?.Expired ?? 0;
            var suspended = stats?.Suspended ?? 0;
            var pending = stats?.Pending ?? 0;
            var girls = stats?.Girls ?? 0;
            var boys = stats?.Boys ?? 0;
            var other = stats?.Other ?? 0;

            var result = new { total, live, expired, suspended, pending, girls, boys, other };
            _cache.Set("StudentCounts", result, TimeSpan.FromMinutes(15));

            return ServiceResult<object>.Ok(result);
        }

        public async Task<ServiceResult<object>> GetStudentsAsync(int page, int pageSize, string search, string status, string scheme, string host, CancellationToken ct = default)
        {
            pageSize = Math.Clamp(pageSize, 1, 100);
            var query = _context.StudentsStudentprofiles
                .AsNoTracking()
                .Where(s => !s.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                var upperStatus = status.ToUpper();
                query = query.Where(s => s.Status == upperStatus);
            }

            if (!string.IsNullOrEmpty(search))
            {
                var likeSearch = $"%{search}%";
                query = query.Where(s => EF.Functions.ILike(s.User.FirstName ?? "", likeSearch) || 
                                         EF.Functions.ILike(s.User.LastName ?? "", likeSearch) || 
                                         (s.User.Email != null && EF.Functions.ILike(s.User.Email ?? "", likeSearch)) ||
                                         (s.User.Mobile != null && EF.Functions.ILike(s.User.Mobile ?? "", likeSearch)) ||
                                         EF.Functions.ILike(s.StudentId ?? "", likeSearch));
            }

            var totalCount = await query.CountAsync(ct);
            var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);

            var dbStudents = await query
                .OrderByDescending(s => s.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(s => s.User)
                .ToListAsync(ct);

            var userIds = dbStudents.Select(s => s.UserId).ToList();

            var activeMemberships = await _context.MembershipsMemberships
                .AsNoTracking()
                .Where(m => userIds.Contains(m.StudentId) && m.Status == "active")
                .ToListAsync(ct);

            var students = dbStudents.Select(s => {
                var studentMemberships = activeMemberships.Where(m => m.StudentId == s.UserId).ToList();
                var activeMembership = studentMemberships.OrderByDescending(m => m.EndDate).FirstOrDefault();
                
                return new {
                    id = s.Id,
                    user_id = s.UserId,
                    student_id = s.StudentId,
                    username = s.User?.Username,
                    first_name = s.User?.FirstName,
                    middle_name = s.MiddleName,
                    last_name = s.User?.LastName,
                    email = s.User?.Email,
                    mobile = s.User?.Mobile,
                    is_active = s.User?.IsActive ?? false,
                    goal = s.Goal,
                    dob = s.Dob,
                    gender = s.Gender,
                    caste = s.Caste,
                    address = s.Address,
                    profile_photo = !string.IsNullOrEmpty(s.ProfilePhoto) ? (s.ProfilePhoto.StartsWith("http") ? s.ProfilePhoto : $"{scheme}://{host}/media/{s.ProfilePhoto}") : null,
                    profile_image = !string.IsNullOrEmpty(s.ProfilePhoto) ? (s.ProfilePhoto.StartsWith("http") ? s.ProfilePhoto : $"{scheme}://{host}/media/{s.ProfilePhoto}") : null,
                    parent_mobile = s.ParentMobile,
                    status = s.Status,
                    suspension_reason = s.SuspensionReason,
                    suspended_at = s.SuspendedAt,
                    preferred_language = s.PreferredLanguage,
                    created_at = s.CreatedAt,
                    updated_at = s.UpdatedAt,
                    joining_date = s.JoiningDate,
                    membership_start_date = activeMembership?.StartDate.ToString("yyyy-MM-dd"),
                    membership_end_date = activeMembership?.EndDate.ToString("yyyy-MM-dd")
            };
            }).ToList();


            return ServiceResult<object>.Ok(new {
                count = totalCount, 
                total_pages = totalPages, 
                current_page = page, 
                next = page < totalPages ? $"/api/v1/admin/students?page={page + 1}&page_size={pageSize}" : null, 
                previous = page > 1 ? $"/api/v1/admin/students?page={page - 1}&page_size={pageSize}" : null, 
                data = students 
            });
        }

        public async Task<ServiceResult<object>> GetStudentDetailAsync(string pk, string scheme, string host, CancellationToken ct = default)
        {
            var dbStudent = await _context.StudentsStudentprofiles
                .AsNoTracking()
                .Include(s => s.User)
                .Where(s => !s.IsDeleted && (s.User.Username == pk || s.User.Id.ToString() == pk || s.StudentId == pk))
                .Select(s => new {
                    id = s.Id,
                    user_id = s.UserId,
                    student_id = s.StudentId,
                    username = s.User.Username,
                    first_name = s.User.FirstName,
                    middle_name = s.MiddleName,
                    last_name = s.User.LastName,
                    email = s.User.Email,
                    mobile = s.User.Mobile,
                    is_active = s.User.IsActive,
                    goal = s.Goal,
                    dob = s.Dob,
                    gender = s.Gender,
                    caste = s.Caste,
                    address = s.Address,
                    profile_photo = s.ProfilePhoto,
                    parent_mobile = s.ParentMobile,
                    status = s.Status,
                    suspension_reason = s.SuspensionReason,
                    suspended_at = s.SuspendedAt,
                    preferred_language = s.PreferredLanguage,
                    created_at = s.CreatedAt,
                    updated_at = s.UpdatedAt,
                    joining_date = s.JoiningDate
                }).FirstOrDefaultAsync(ct);

            if (dbStudent == null) return ServiceResult<object>.NotFound("Student not found");

            var student = new {
                id = dbStudent.id,
                user_id = dbStudent.user_id,
                student_id = dbStudent.student_id,
                username = dbStudent.username,
                first_name = dbStudent.first_name,
                middle_name = dbStudent.middle_name,
                last_name = dbStudent.last_name,
                email = dbStudent.email,
                mobile = dbStudent.mobile,
                is_active = dbStudent.is_active,
                goal = dbStudent.goal,
                dob = dbStudent.dob,
                gender = dbStudent.gender,
                caste = dbStudent.caste,
                address = dbStudent.address,
                profile_photo = !string.IsNullOrEmpty(dbStudent.profile_photo) ? (dbStudent.profile_photo.StartsWith("http") ? dbStudent.profile_photo : $"{scheme}://{host}/media/{dbStudent.profile_photo}") : null,
                profile_image = !string.IsNullOrEmpty(dbStudent.profile_photo) ? (dbStudent.profile_photo.StartsWith("http") ? dbStudent.profile_photo : $"{scheme}://{host}/media/{dbStudent.profile_photo}") : null,
                parent_mobile = dbStudent.parent_mobile,
                status = dbStudent.status,
                suspension_reason = dbStudent.suspension_reason,
                suspended_at = dbStudent.suspended_at,
                preferred_language = dbStudent.preferred_language,
                created_at = dbStudent.created_at,
                updated_at = dbStudent.updated_at,
                joining_date = dbStudent.joining_date
            };

            if (student == null) return ServiceResult<object>.NotFound("Student not found");

            return ServiceResult<object>.Ok(student);
        }

        public async Task<ServiceResult<object>> CreateStudentAsync(WebApplication1.Models.DTOs.Admin.StudentPayload payload, CancellationToken ct = default)
        {
            var validationErrors = new Dictionary<string, string[]>();

            if (!string.IsNullOrWhiteSpace(payload.Email))
            {
                if (await _context.AccountsCustomusers.AnyAsync(u => u.Email == payload.Email, ct))
                {
                    validationErrors.Add("email", new[] { "A user with that email already exists." });
                }
            }

            if (!string.IsNullOrWhiteSpace(payload.Mobile))
            {
                if (await _context.AccountsCustomusers.AnyAsync(u => u.Mobile == payload.Mobile, ct))
                {
                    validationErrors.Add("mobile", new[] { "A user with that mobile number already exists." });
                }
            }

            if (validationErrors.Any())
            {
                return ServiceResult<object>.Fail("Validation failed", validationErrors);
            }

            string username = payload.Username ?? payload.Mobile ?? $"{payload.FirstName?.ToLower() ?? "student"}{System.Security.Cryptography.RandomNumberGenerator.GetInt32(1000, 10000)}";

            if (await _context.AccountsCustomusers.AnyAsync(u => u.Username == username, ct))
            {
                username = $"{username}{System.Security.Cryptography.RandomNumberGenerator.GetInt32(100, 1000)}";
                if (await _context.AccountsCustomusers.AnyAsync(u => u.Username == username, ct))
                    return ServiceResult<object>.Fail("Validation failed", new Dictionary<string, string[]> { { "mobile", new[] { "Username or mobile already exists." } } });
            }

            var newUser = new AccountsCustomuser
            {
                Username = username,
                FirstName = payload.FirstName ?? "",
                LastName = payload.LastName ?? "",
                Email = string.IsNullOrWhiteSpace(payload.Email) ? null : payload.Email,
                Mobile = string.IsNullOrWhiteSpace(payload.Mobile) ? null : payload.Mobile,
                Role = WebApplication1.Utils.Constants.Roles.Student,
                IsActive = payload.IsActive ?? true,
                DateJoined = DateTime.UtcNow,
                Password = Utils.PasswordHasher.HashDjangoPassword(string.IsNullOrWhiteSpace(payload.Password) ? "shresht@123" : payload.Password)
            };

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                var isRelational = _context.Database.IsRelational();
                var transaction = isRelational ? await _context.Database.BeginTransactionAsync(ct) : null;
            try
            {
                _context.AccountsCustomusers.Add(newUser);
                try 
                {
                    await _context.SaveChangesAsync(ct);
                }
                catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505")
                {
                    if (transaction != null) await transaction.RollbackAsync(ct);
                    return ServiceResult<object>.Fail("Validation failed", new Dictionary<string, string[]> { { "mobile_or_email", new[] { "Username, mobile, or email already exists." } } });
                }

                var newProfile = new StudentsStudentprofile
                {
                    UserId = newUser.Id,
                    StudentId = $"SHR-{newUser.Id:D4}",
                    Goal = payload.Goal ?? "",
                    Gender = payload.Gender ?? "Male",
                    Status = string.IsNullOrWhiteSpace(payload.Status) ? WebApplication1.Utils.Constants.StudentStatus.Pending : payload.Status,
                    JoiningDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    PreferredLanguage = payload.PreferredLanguage ?? "English",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Address = payload.Address,
                    Caste = payload.Caste,
                    ParentMobile = payload.ParentMobile,
                    MiddleName = payload.MiddleName
                };

                if (!string.IsNullOrEmpty(payload.Dob) && DateOnly.TryParse(payload.Dob, out var dob))
                    newProfile.Dob = dob;

                _context.StudentsStudentprofiles.Add(newProfile);
                await _context.SaveChangesAsync(ct);
                if (transaction != null) await transaction.CommitAsync(ct);

                if (!string.IsNullOrWhiteSpace(payload.Email) || !string.IsNullOrWhiteSpace(payload.Mobile))
                {
                    var email = payload.Email ?? "";
                    var fName = payload.FirstName ?? "";
                    var lName = payload.LastName ?? "";
                    try {
                        using var scope = _scopeFactory.CreateScope();
                        if (!string.IsNullOrWhiteSpace(email))
                        {
                            var emailSvc = scope.ServiceProvider.GetRequiredService<IEmailService>();
                            await emailSvc.SendWelcomeEmailAsync(email, fName, lName);
                        }
                        
                        var dispatcher = scope.ServiceProvider.GetRequiredService<INotificationDispatcher>();
                        string msg = $"🎉 Welcome to Shresht Library, {fName}!\nYour student ID is {newProfile.StudentId}. You can login using this ID/mobile number.";
                        await dispatcher.SendToStudentAsync(newUser.Id, "Welcome to Shresht Library 🎉", "Your account has been created successfully.", WebApplication1.Utils.NotificationTypes.Account, whatsappMessage: msg);
                    } catch (Exception ex) { 
                        _logger.LogError(ex, "Error sending welcome notification on admin create");
                    }
                }

                return ServiceResult<object>.Ok(new { id = newProfile.Id, user_id = newUser.Id, student_id = newProfile.StudentId });
            }
            catch (Exception)
            {
                if (transaction != null) await transaction.RollbackAsync(ct);
                throw;
            }
            finally
            {
                if (transaction != null) await transaction.DisposeAsync();
            }
            });
        }

        public async Task<ServiceResult<object>> UpdateStudentAsync(string pk, WebApplication1.Models.DTOs.Admin.StudentPayload payload, CancellationToken ct = default)
        {
            var student = await _context.AccountsCustomusers.Include(u => u.StudentsStudentprofile).FirstOrDefaultAsync(u => u.StudentsStudentprofile != null && !u.IsDeleted && (u.Username == pk || u.Id.ToString() == pk || u.StudentsStudentprofile.StudentId == pk), ct);
            if (student == null) return ServiceResult<object>.NotFound("Student not found");

            var validationErrors = new Dictionary<string, string[]>();

            if (payload.Email != null) 
            {
                if (!string.IsNullOrWhiteSpace(payload.Email) && payload.Email != student.Email)
                {
                    if (await _context.AccountsCustomusers.AnyAsync(u => u.Email == payload.Email, ct))
                    {
                        validationErrors.Add("email", new[] { "A user with that email already exists." });
                    }
                }
            }
            if (payload.Mobile != null) 
            {
                if (!string.IsNullOrWhiteSpace(payload.Mobile) && payload.Mobile != student.Mobile)
                {
                    if (await _context.AccountsCustomusers.AnyAsync(u => u.Mobile == payload.Mobile, ct))
                    {
                        validationErrors.Add("mobile", new[] { "A user with that mobile number already exists." });
                    }
                }
            }

            if (validationErrors.Any())
            {
                return ServiceResult<object>.Fail("Validation failed", validationErrors);
            }

            if (payload.FirstName != null) student.FirstName = payload.FirstName;
            if (payload.LastName != null) student.LastName = payload.LastName;
            if (payload.Email != null) student.Email = string.IsNullOrWhiteSpace(payload.Email) ? null : payload.Email;
            if (payload.Mobile != null) student.Mobile = string.IsNullOrWhiteSpace(payload.Mobile) ? null : payload.Mobile;

            if (payload.IsActive.HasValue) student.IsActive = payload.IsActive.Value;

            if (student.StudentsStudentprofile != null)
            {
                if (payload.Goal != null) student.StudentsStudentprofile.Goal = payload.Goal;
                if (payload.Gender != null) student.StudentsStudentprofile.Gender = payload.Gender;
                if (payload.Status != null) student.StudentsStudentprofile.Status = payload.Status;
                if (payload.PreferredLanguage != null) student.StudentsStudentprofile.PreferredLanguage = payload.PreferredLanguage;
                if (payload.Address != null) student.StudentsStudentprofile.Address = payload.Address;
                if (payload.Caste != null) student.StudentsStudentprofile.Caste = payload.Caste;
                if (payload.ParentMobile != null) student.StudentsStudentprofile.ParentMobile = payload.ParentMobile;
                if (payload.MiddleName != null) student.StudentsStudentprofile.MiddleName = payload.MiddleName;
                
                if (!string.IsNullOrEmpty(payload.Dob) && DateOnly.TryParse(payload.Dob, out var dob))
                    student.StudentsStudentprofile.Dob = dob;
                    
                student.StudentsStudentprofile.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(ct);
            _cache.Remove($"StudentProfile_{student.Id}");
            _cache.Remove($"StudentDashboard_{student.Id}");
            return ServiceResult<object>.Ok(new { id = student.StudentsStudentprofile?.Id });
        }

        public async Task<ServiceResult<bool>> DeleteStudentAsync(string pk, CancellationToken ct = default)
        {
            var student = await _context.AccountsCustomusers.Include(u => u.StudentsStudentprofile).FirstOrDefaultAsync(u => u.StudentsStudentprofile != null && !u.IsDeleted && (u.Username == pk || u.Id.ToString() == pk || u.StudentsStudentprofile.StudentId == pk), ct);
            if (student == null) return ServiceResult<bool>.NotFound("Student not found");

            var userId = student.Id;
            
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync(ct);
                try
                {
                    // Soft delete user and profile
                    student.IsDeleted = true;
                    student.IsActive = false;
                    
                    if (student.StudentsStudentprofile != null)
                    {
                        student.StudentsStudentprofile.IsDeleted = true;
                    }

                    // Nullify foreign keys that shouldn't be soft deleted (e.g., active seat)
                    await _context.SeatsSeats.Where(s => s.StudentId == userId)
                        .ExecuteUpdateAsync(s => s.SetProperty(p => p.StudentId, (int?)null), ct);

                    // Clear out tokens
                    var tokenIds = _context.TokenBlacklistOutstandingtokens.Where(x => x.UserId == userId).Select(x => x.Id);
                    await _context.TokenBlacklistBlacklistedtokens.Where(x => tokenIds.Contains(x.TokenId)).ExecuteDeleteAsync(ct);
                    await _context.TokenBlacklistOutstandingtokens.Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);

                    await _context.SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);
                    
                    return ServiceResult<bool>.Ok(true);
                }
            catch (Exception)
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
            });
        }

        public async Task<ServiceResult<object>> GetStudentAnalyticsAsync(string pk, string? period = "weekly", CancellationToken ct = default)
        {
            var profile = await _context.StudentsStudentprofiles
                .AsNoTracking()
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.User.Username == pk || s.User.Id.ToString() == pk || s.StudentId == pk, ct);

            if (profile == null) return ServiceResult<object>.NotFound("Student not found");

            var targetDailyHours = profile.AllowedStudyMinutes.HasValue && profile.AllowedStudyMinutes.Value > 0
                ? Math.Round(profile.AllowedStudyMinutes.Value / 60.0, 2)
                : 6.0;

            var selectedPeriod = (period ?? "weekly").ToLowerInvariant();
            var nowIst = _dateTimeProvider.IstNow;
            var todayIstDate = DateOnly.FromDateTime(nowIst);

            var attendanceList = new System.Collections.Generic.List<object>();
            var studyList = new System.Collections.Generic.List<object>();

            if (selectedPeriod == "yearly")
            {
                var startMonth = new DateTime(nowIst.Year, nowIst.Month, 1).AddMonths(-11);
                var startMonthDate = DateOnly.FromDateTime(startMonth);
                var startUtc = DateTime.SpecifyKind(startMonth, DateTimeKind.Utc);

                var sessions = await _context.StudyStudysessions
                    .AsNoTracking()
                    .Where(s => s.StudentId == profile.UserId && s.StartTime >= startUtc && (s.Status == "completed" || s.Status == "ended"))
                    .ToListAsync(ct);

                var attendances = await _context.AttendanceAttendances
                    .AsNoTracking()
                    .Where(a => a.StudentId == profile.UserId && a.Date >= startMonthDate)
                    .ToListAsync(ct);

                for (int i = 0; i < 12; i++)
                {
                    var m = startMonth.AddMonths(i);
                    var mStart = new DateOnly(m.Year, m.Month, 1);
                    var mEnd = mStart.AddMonths(1).AddDays(-1);
                    var monthLabel = m.ToString("MMM yyyy");

                    var monthAtt = attendances.Where(a => a.Date >= mStart && a.Date <= mEnd).ToList();
                    int presentCount = monthAtt.Count(a => a.IsPresent);
                    int absentCount = monthAtt.Count(a => !a.IsPresent);

                    attendanceList.Add(new
                    {
                        label = monthLabel,
                        present = presentCount,
                        absent = absentCount,
                        total = monthAtt.Count
                    });

                    double sessionMinutes = sessions
                        .Where(s => {
                            var istTime = TimeZoneInfo.ConvertTimeFromUtc(s.StartTime, _dateTimeProvider.IstTimeZone);
                            return istTime.Year == m.Year && istTime.Month == m.Month;
                        })
                        .Sum(s => s.DurationMinutes);

                    double sessionHours = sessionMinutes / 60.0;
                    double attHours = 0;
                    foreach (var a in monthAtt.Where(a => a.IsPresent))
                    {
                        attHours += ParseHours(a.TotalHours);
                    }

                    double finalHours = Math.Max(sessionHours, attHours);
                    double monthTargetHours = Math.Round(targetDailyHours * 25, 2);

                    studyList.Add(new
                    {
                        label = monthLabel,
                        hours = Math.Round(finalHours, 2),
                        target_hours = monthTargetHours
                    });
                }
            }
            else if (selectedPeriod == "monthly")
            {
                var startDate = todayIstDate.AddDays(-29);
                var startUtc = DateTime.SpecifyKind(startDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

                var sessions = await _context.StudyStudysessions
                    .AsNoTracking()
                    .Where(s => s.StudentId == profile.UserId && s.StartTime >= startUtc && (s.Status == "completed" || s.Status == "ended"))
                    .ToListAsync(ct);

                var attendances = await _context.AttendanceAttendances
                    .AsNoTracking()
                    .Where(a => a.StudentId == profile.UserId && a.Date >= startDate)
                    .ToListAsync(ct);

                for (int i = 0; i < 30; i++)
                {
                    var d = startDate.AddDays(i);
                    var dateLabel = d.ToString("dd MMM");

                    var dayAtt = attendances.Where(a => a.Date == d).ToList();
                    int presentCount = dayAtt.Count(a => a.IsPresent);
                    int absentCount = dayAtt.Count(a => !a.IsPresent);

                    attendanceList.Add(new
                    {
                        label = dateLabel,
                        present = presentCount,
                        absent = absentCount,
                        total = dayAtt.Count
                    });

                    double sessionMinutes = sessions
                        .Where(s => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(s.StartTime, _dateTimeProvider.IstTimeZone)) == d)
                        .Sum(s => s.DurationMinutes);

                    double sessionHours = sessionMinutes / 60.0;
                    double attHours = dayAtt.Where(a => a.IsPresent).Sum(a => ParseHours(a.TotalHours));
                    double finalHours = Math.Max(sessionHours, attHours);

                    studyList.Add(new
                    {
                        label = dateLabel,
                        hours = Math.Round(finalHours, 2),
                        target_hours = targetDailyHours
                    });
                }
            }
            else // "weekly"
            {
                var startDate = todayIstDate.AddDays(-6);
                var startUtc = DateTime.SpecifyKind(startDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

                var sessions = await _context.StudyStudysessions
                    .AsNoTracking()
                    .Where(s => s.StudentId == profile.UserId && s.StartTime >= startUtc && (s.Status == "completed" || s.Status == "ended"))
                    .ToListAsync(ct);

                var attendances = await _context.AttendanceAttendances
                    .AsNoTracking()
                    .Where(a => a.StudentId == profile.UserId && a.Date >= startDate)
                    .ToListAsync(ct);

                for (int i = 0; i < 7; i++)
                {
                    var d = startDate.AddDays(i);
                    var dateLabel = d.ToString("ddd (dd/MM)");

                    var dayAtt = attendances.Where(a => a.Date == d).ToList();
                    int presentCount = dayAtt.Count(a => a.IsPresent);
                    int absentCount = dayAtt.Count(a => !a.IsPresent);

                    attendanceList.Add(new
                    {
                        label = dateLabel,
                        present = presentCount,
                        absent = absentCount,
                        total = dayAtt.Count
                    });

                    double sessionMinutes = sessions
                        .Where(s => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(s.StartTime, _dateTimeProvider.IstTimeZone)) == d)
                        .Sum(s => s.DurationMinutes);

                    double sessionHours = sessionMinutes / 60.0;
                    double attHours = dayAtt.Where(a => a.IsPresent).Sum(a => ParseHours(a.TotalHours));
                    double finalHours = Math.Max(sessionHours, attHours);

                    studyList.Add(new
                    {
                        label = dateLabel,
                        hours = Math.Round(finalHours, 2),
                        target_hours = targetDailyHours
                    });
                }
            }

            return ServiceResult<object>.Ok(new { period = selectedPeriod, attendance = attendanceList, study = studyList });
        }

        private static double ParseHours(string? input)
        {
            if (string.IsNullOrEmpty(input)) return 0;
            if (input.Contains(':'))
            {
                var parts = input.Split(':');
                if (parts.Length >= 2 && int.TryParse(parts[0], out var hrs) && int.TryParse(parts[1], out var mins))
                {
                    return hrs + (mins / 60.0);
                }
            }
            else if (double.TryParse(input, out var h))
            {
                return h;
            }
            return 0;
        }

        public async Task<ServiceResult<object>> SuspendStudentAsync(string pk, string? reason, CancellationToken ct = default)
        {
            var student = await _context.AccountsCustomusers.Include(u => u.StudentsStudentprofile).FirstOrDefaultAsync(u => u.StudentsStudentprofile != null && !u.IsDeleted && (u.Username == pk || u.Id.ToString() == pk || u.StudentsStudentprofile.StudentId == pk), ct);
            if (student == null) return ServiceResult<object>.NotFound("Student not found");

            student.StudentsStudentprofile!.Status = WebApplication1.Utils.Constants.StudentStatus.Suspended;
            student.StudentsStudentprofile.SuspendedAt = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(reason))
            {
                student.StudentsStudentprofile.SuspensionReason = reason;
            }
            await _context.SaveChangesAsync(ct);
            _cache.Remove($"StudentProfile_{student.Id}");
            _cache.Remove($"StudentDashboard_{student.Id}");

            var email = student.Email;
            var suspensionReason = student.StudentsStudentprofile?.SuspensionReason ?? "";

            _ = Task.Run(async () => 
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var emailSvc = scope.ServiceProvider.GetRequiredService<IEmailService>();
                    if (!string.IsNullOrWhiteSpace(email))
                    {
                        await emailSvc.SendSuspendedEmailAsync(email, suspensionReason);
                    }
                    
                    var dispatcher = scope.ServiceProvider.GetRequiredService<INotificationDispatcher>();
                    string msg = $"⚠️ Your library account has been suspended.\nReason: {(string.IsNullOrEmpty(suspensionReason) ? "Policy violation or unpaid dues" : suspensionReason)}\nContact Admin for details.";
                    await dispatcher.SendToStudentAsync(student.Id, "Account Suspended", string.IsNullOrEmpty(suspensionReason) ? "Your account has been suspended." : suspensionReason, WebApplication1.Utils.NotificationTypes.Account, whatsappMessage: msg);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending suspend notification");
                }
            });

            return ServiceResult<object>.Ok(new { student_id = pk, status = WebApplication1.Utils.Constants.StudentStatus.Suspended });
        }

        public async Task<ServiceResult<object>> ActivateStudentAsync(string pk, CancellationToken ct = default)
        {
            var student = await _context.AccountsCustomusers.Include(u => u.StudentsStudentprofile).FirstOrDefaultAsync(u => u.StudentsStudentprofile != null && !u.IsDeleted && (u.Username == pk || u.Id.ToString() == pk || u.StudentsStudentprofile.StudentId == pk), ct);
            if (student == null) return ServiceResult<object>.NotFound("Student not found");

            student.StudentsStudentprofile!.Status = WebApplication1.Utils.Constants.StudentStatus.Live;
            student.StudentsStudentprofile.SuspendedAt = null;
            student.StudentsStudentprofile.SuspensionReason = null;
            await _context.SaveChangesAsync(ct);
            _cache.Remove($"StudentProfile_{student.Id}");
            _cache.Remove($"StudentDashboard_{student.Id}");

            _ = Task.Run(async () => 
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var emailSvc = scope.ServiceProvider.GetRequiredService<IEmailService>();
                    if (!string.IsNullOrWhiteSpace(student.Email))
                    {
                        await emailSvc.SendActivatedEmailAsync(student.Email);
                    }
                    
                    var dispatcher = scope.ServiceProvider.GetRequiredService<INotificationDispatcher>();
                    string msg = "✅ Good news! Your Shresht Library account has been successfully reactivated. You can now access library facilities again.";
                    await dispatcher.SendToStudentAsync(student.Id, "Account Activated ✅", "Your account has been successfully reactivated.", WebApplication1.Utils.NotificationTypes.Account, whatsappMessage: msg);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending activate notification");
                }
            });

            return ServiceResult<object>.Ok(new { student_id = pk, status = WebApplication1.Utils.Constants.StudentStatus.Live });
        }
        
        public async Task<ServiceResult<object>> GetStudentRelatedDataAsync(string pk, string kind, CancellationToken ct = default)
        {
            var student = await _context.AccountsCustomusers
                .AsNoTracking()
                .Include(u => u.StudentsStudentprofile)
                .FirstOrDefaultAsync(u => u.StudentsStudentprofile != null && !u.IsDeleted && 
                    (u.Username == pk || u.Id.ToString() == pk || u.StudentsStudentprofile.StudentId == pk), ct);
            
            if (student == null) return ServiceResult<object>.NotFound("Student not found");

            if (kind == "attendance")
            {
                var data = await _context.AttendanceAttendances.Where(a => a.StudentId == student.Id).OrderByDescending(a => a.Date).Take(30).Select(a => new {
                    id = a.Id, date = a.Date, is_present = a.IsPresent, time_in = a.MarkedAt.HasValue ? TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(a.MarkedAt.Value, _dateTimeProvider.IstTimeZone)) : a.TimeIn, time_out = a.TimeOut, total_hours = a.TotalHours
                }).ToListAsync(ct);
                return ServiceResult<object>.Ok(data);
            }
            else if (kind == "payments")
            {
                var data = await _context.PaymentsPayments.Include(p => p.Membership).ThenInclude(m => m!.Plan).Where(p => p.StudentId == student.Id).OrderByDescending(p => p.PaymentDate).Take(50).Select(p => new {
                    id = p.Id, amount = p.Amount, payment_date = p.PaymentDate, payment_method = p.PaymentMode, status = p.Status, transaction_id = p.TransactionId, plan_name = p.Membership != null && p.Membership.Plan != null ? p.Membership.Plan.Name : null
                }).ToListAsync(ct);
                return ServiceResult<object>.Ok(data);
            }
            else if (kind == "memberships")
            {
                var data = await _context.MembershipsMemberships.Include(m => m.Plan).Where(m => m.StudentId == student.Id).OrderByDescending(m => m.StartDate).Take(50).Select(m => new {
                    id = m.Id, start_date = m.StartDate, end_date = m.EndDate, status = m.Status, amount_paid = m.PriceSnapshot,
                    plan_name = m.Plan != null ? m.Plan.Name : null
                }).ToListAsync(ct);
                return ServiceResult<object>.Ok(data);
            }
            else if (kind == "timeline")
            {
                return ServiceResult<object>.Ok(new object[] { });
            }

            return ServiceResult<object>.Fail("Invalid kind");
        }

        public async Task<ServiceResult<object>> UploadStudentPhotoAsync(string pk, Microsoft.AspNetCore.Http.IFormFile photo, string scheme, string host, CancellationToken ct = default)
        {
            var student = await _context.AccountsCustomusers.Include(u => u.StudentsStudentprofile).FirstOrDefaultAsync(u => u.StudentsStudentprofile != null && !u.IsDeleted && (u.Username == pk || u.Id.ToString() == pk || u.StudentsStudentprofile.StudentId == pk), ct);
            if (student == null) return ServiceResult<object>.NotFound("Student not found");

            if (photo == null || photo.Length == 0)
                return ServiceResult<object>.Fail("No file uploaded");

            var cloudinaryUrl = await _cloudinary.UploadImageAsync(photo, "profiles");
            if (string.IsNullOrEmpty(cloudinaryUrl))
                return ServiceResult<object>.Fail("Failed to upload photo to Cloudinary");

            student.StudentsStudentprofile!.ProfilePhoto = cloudinaryUrl;
            student.StudentsStudentprofile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
            _cache.Remove($"StudentProfile_{student.Id}");
            _cache.Remove($"StudentDashboard_{student.Id}");

            return ServiceResult<object>.Ok(new { 
                profile_photo = cloudinaryUrl 
            });
        }

        public async Task<ServiceResult<object>> ExportStudentsAsync(CancellationToken ct = default)
        {
            var students = await _context.StudentsStudentprofiles
                .AsNoTracking()
                .Where(s => !s.IsDeleted)
                .Include(s => s.User)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new {
                    student_id = s.StudentId,
                    username = s.User.Username,
                    first_name = s.User.FirstName,
                    middle_name = s.MiddleName,
                    last_name = s.User.LastName,
                    mobile = s.User.Mobile,
                    email = s.User.Email,
                    gender = s.Gender,
                    dob = s.Dob,
                    goal = s.Goal,
                    caste = s.Caste,
                    address = s.Address,
                    parent_mobile = s.ParentMobile,
                    preferred_language = s.PreferredLanguage,
                    status = s.Status,
                    joining_date = s.JoiningDate
                })
                .ToListAsync(ct);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Student ID,Username,First Name,Middle Name,Last Name,Mobile,Email,Gender,Date of Birth,Goal,Caste,Address,Parent Mobile,Preferred Language,Status,Joining Date");
            foreach(var s in students)
            {
                var fName = s.first_name?.Replace(",", " ") ?? "";
                var mName = s.middle_name?.Replace(",", " ") ?? "";
                var lName = s.last_name?.Replace(",", " ") ?? "";
                var address = s.address?.Replace(",", " ")?.Replace("\n", " ")?.Replace("\r", "") ?? "";
                var email = s.email?.Replace(",", " ") ?? "";
                var status = s.status ?? "";
                var goal = s.goal?.Replace(",", " ") ?? "";
                var dob = s.dob.HasValue ? s.dob.Value.ToString("yyyy-MM-dd") : "";
                var joinDate = s.joining_date.ToString("yyyy-MM-dd");
                var uName = s.username?.Replace(",", " ") ?? "";

                sb.AppendLine($"{s.student_id},{uName},{fName},{mName},{lName},{s.mobile},{email},{s.gender},{dob},{goal},{s.caste},{address},{s.parent_mobile},{s.preferred_language},{status},{joinDate}");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            return ServiceResult<object>.Ok(bytes);
        }
    }
}
