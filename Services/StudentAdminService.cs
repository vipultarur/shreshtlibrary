using System;
using System.Collections.Generic;
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
    public class StudentAdminService : IStudentAdminService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IMemoryCache _cache;

        public StudentAdminService(ApplicationDbContext context, IEmailService emailService, IMemoryCache cache)
        {
            _context = context;
            _emailService = emailService;
            _cache = cache;
        }

        public async Task<ServiceResult<object>> GetStudentCountsAsync(CancellationToken ct = default)
        {
            if (_cache.TryGetValue("StudentCounts", out var cachedResult))
            {
                return ServiceResult<object>.Ok(cachedResult!);
            }

            var total = await _context.AccountsCustomusers.CountAsync(u => u.Role == WebApplication1.Utils.Constants.Roles.Student, ct);
            
            var stats = await _context.StudentsStudentprofiles
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
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                var upperStatus = status.ToUpper();
                query = query.Where(s => s.Status == upperStatus);
            }

            if (!string.IsNullOrEmpty(search))
            {
                var likeSearch = $"%{search}%";
                query = query.Where(s => EF.Functions.ILike(s.User.FirstName, likeSearch) || 
                                         EF.Functions.ILike(s.User.LastName, likeSearch) || 
                                         (s.User.Mobile != null && EF.Functions.ILike(s.User.Mobile, likeSearch)));
            }

            var totalCount = await query.CountAsync(ct);
            var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);

            var dbStudents = await query
                .OrderByDescending(s => s.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
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
                    joining_date = s.JoiningDate,
                    membership_start_date = _context.MembershipsMemberships
                        .Where(m => m.StudentId == s.UserId && m.Status == "active")
                        .OrderByDescending(m => m.EndDate)
                        .Select(m => (DateOnly?)m.StartDate)
                        .FirstOrDefault(),
                    membership_end_date = _context.MembershipsMemberships
                        .Where(m => m.StudentId == s.UserId && m.Status == "active")
                        .OrderByDescending(m => m.EndDate)
                        .Select(m => (DateOnly?)m.EndDate)
                        .FirstOrDefault()
                })
                .ToListAsync(ct);

            var students = dbStudents.Select(s => new {
                    id = s.id,
                    user_id = s.user_id,
                    student_id = s.student_id,
                    username = s.username,
                    first_name = s.first_name,
                    middle_name = s.middle_name,
                    last_name = s.last_name,
                    email = s.email,
                    mobile = s.mobile,
                    is_active = s.is_active,
                    goal = s.goal,
                    dob = s.dob,
                    gender = s.gender,
                    caste = s.caste,
                    address = s.address,
                    profile_photo = !string.IsNullOrEmpty(s.profile_photo) ? $"{scheme}://{host}/media/{s.profile_photo}" : null,
                    profile_image = !string.IsNullOrEmpty(s.profile_photo) ? $"{scheme}://{host}/media/{s.profile_photo}" : null,
                    parent_mobile = s.parent_mobile,
                    status = s.status,
                    suspension_reason = s.suspension_reason,
                    suspended_at = s.suspended_at,
                    preferred_language = s.preferred_language,
                    created_at = s.created_at,
                    updated_at = s.updated_at,
                    joining_date = s.joining_date,
                    membership_start_date = s.membership_start_date?.ToString("yyyy-MM-dd"),
                    membership_end_date = s.membership_end_date?.ToString("yyyy-MM-dd")
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
                .Where(s => s.User.Username == pk || s.User.Id.ToString() == pk || s.StudentId == pk)
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
                profile_photo = !string.IsNullOrEmpty(dbStudent.profile_photo) ? $"{scheme}://{host}/media/{dbStudent.profile_photo}" : null,
                profile_image = !string.IsNullOrEmpty(dbStudent.profile_photo) ? $"{scheme}://{host}/media/{dbStudent.profile_photo}" : null,
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

        public async Task<ServiceResult<object>> CreateStudentAsync(AdminStudentsController.StudentPayload payload, CancellationToken ct = default)
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

            string username = payload.Username ?? payload.Mobile ?? $"{payload.FirstName?.ToLower() ?? "student"}{new Random().Next(1000, 9999)}";

            if (await _context.AccountsCustomusers.AnyAsync(u => u.Username == username, ct))
            {
                username = $"{username}{new Random().Next(100, 999)}";
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
                await _context.SaveChangesAsync(ct);

                var newProfile = new StudentsStudentprofile
                {
                    UserId = newUser.Id,
                    StudentId = $"SHR-{newUser.Id:D4}",
                    Goal = payload.Goal ?? "",
                    Gender = payload.Gender ?? "Other",
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

                if (!string.IsNullOrWhiteSpace(payload.Email))
                {
                    var email = payload.Email!;
                    var fName = payload.FirstName ?? "";
                    var lName = payload.LastName ?? "";
                    _ = Task.Run(() => _emailService.SendWelcomeEmailAsync(email, fName, lName));
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

        public async Task<ServiceResult<object>> UpdateStudentAsync(string pk, AdminStudentsController.StudentPayload payload, CancellationToken ct = default)
        {
            var student = await _context.AccountsCustomusers.Include(u => u.StudentsStudentprofile).FirstOrDefaultAsync(u => u.StudentsStudentprofile != null && (u.Username == pk || u.Id.ToString() == pk || u.StudentsStudentprofile.StudentId == pk), ct);
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
            return ServiceResult<object>.Ok(new { id = student.StudentsStudentprofile?.Id });
        }

        public async Task<ServiceResult<bool>> DeleteStudentAsync(string pk, CancellationToken ct = default)
        {
            var student = await _context.AccountsCustomusers.Include(u => u.StudentsStudentprofile).FirstOrDefaultAsync(u => u.StudentsStudentprofile != null && (u.Username == pk || u.Id.ToString() == pk || u.StudentsStudentprofile.StudentId == pk), ct);
            if (student == null) return ServiceResult<bool>.NotFound("Student not found");

            var userId = student.Id;
            
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync(ct);
                try
            {
                // Nullify foreign keys that shouldn't be deleted
                await _context.SeatsSeats.Where(s => s.StudentId == userId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.StudentId, (int?)null), ct);

                // Delete related records
                await _context.NotificationsDevicetokens.Where(x => x.StudentId == userId).ExecuteDeleteAsync(ct);
                await _context.AttendanceAttendances.Where(x => x.StudentId == userId).ExecuteDeleteAsync(ct);
                await _context.PaymentsPayments.Where(x => x.StudentId == userId).ExecuteDeleteAsync(ct);
                await _context.MembershipsMemberships.Where(x => x.StudentId == userId).ExecuteDeleteAsync(ct);
                await _context.NotificationsAdmininboxnotifications.Where(x => x.StudentId == userId).ExecuteDeleteAsync(ct);
                await _context.NotificationsStudentnotifications.Where(x => x.StudentId == userId).ExecuteDeleteAsync(ct);
                await _context.StudyStudysessions.Where(x => x.StudentId == userId).ExecuteDeleteAsync(ct);
                await _context.SeatsSeatassignments.Where(x => x.StudentId == userId).ExecuteDeleteAsync(ct);
                await _context.SeatsSeatchangelogs.Where(x => x.StudentId == userId).ExecuteDeleteAsync(ct);
                await _context.LibraryReviews.Where(x => x.StudentId == userId).ExecuteDeleteAsync(ct);
                await _context.StudentsReferralhistories.Where(x => x.ReferredStudentId == userId || x.ReferrerId == userId).ExecuteDeleteAsync(ct);
                await _context.StudentsReferralcodes.Where(x => x.StudentId == userId).ExecuteDeleteAsync(ct);
                
                // Clear out tokens
                var tokenIds = _context.TokenBlacklistOutstandingtokens.Where(x => x.UserId == userId).Select(x => x.Id);
                await _context.TokenBlacklistBlacklistedtokens.Where(x => tokenIds.Contains(x.TokenId)).ExecuteDeleteAsync(ct);
                await _context.TokenBlacklistOutstandingtokens.Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);

                // Clear out auth related rows
                await _context.AccountsCustomuserGroups.Where(x => x.CustomuserId == userId).ExecuteDeleteAsync(ct);
                await _context.AccountsCustomuserUserPermissions.Where(x => x.CustomuserId == userId).ExecuteDeleteAsync(ct);
                await _context.CoreActivitylogs.Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);

                if (student.StudentsStudentprofile != null)
                    _context.StudentsStudentprofiles.Remove(student.StudentsStudentprofile);
                _context.AccountsCustomusers.Remove(student);
                
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

        public async Task<ServiceResult<object>> GetStudentAnalyticsAsync(string pk, CancellationToken ct = default)
        {
            var profile = await _context.StudentsStudentprofiles.AsNoTracking().Include(s => s.User).FirstOrDefaultAsync(s => s.User.Username == pk || s.User.Id.ToString() == pk || s.StudentId == pk, ct);
            if (profile == null) return ServiceResult<object>.NotFound("Student not found");

            var thirtyDaysAgo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
            var attendances = await _context.AttendanceAttendances
                .Where(a => a.StudentId == profile.UserId && a.Date >= thirtyDaysAgo)
                .OrderBy(a => a.Date)
                .Select(a => new {
                    date = a.Date.ToString("yyyy-MM-dd"),
                    is_present = a.IsPresent,
                    time_in = a.TimeIn,
                    time_out = a.TimeOut,
                    total_hours = a.TotalHours
                })
                .ToListAsync(ct);

            return ServiceResult<object>.Ok(new { period = "monthly", attendance = attendances, study = new object[] { } });
        }

        public async Task<ServiceResult<object>> SuspendStudentAsync(string pk, string? reason, CancellationToken ct = default)
        {
            var student = await _context.AccountsCustomusers.Include(u => u.StudentsStudentprofile).FirstOrDefaultAsync(u => u.StudentsStudentprofile != null && (u.Username == pk || u.Id.ToString() == pk || u.StudentsStudentprofile.StudentId == pk), ct);
            if (student == null) return ServiceResult<object>.NotFound("Student not found");

            student.StudentsStudentprofile!.Status = WebApplication1.Utils.Constants.StudentStatus.Suspended;
            student.StudentsStudentprofile.SuspendedAt = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(reason))
            {
                student.StudentsStudentprofile.SuspensionReason = reason;
            }
            await _context.SaveChangesAsync(ct);

            if (!string.IsNullOrWhiteSpace(student.Email))
            {
                var email = student.Email!;
                var suspensionReason = student.StudentsStudentprofile?.SuspensionReason ?? "";
                _ = Task.Run(() => _emailService.SendSuspendedEmailAsync(email, suspensionReason));
            }

            return ServiceResult<object>.Ok(new { student_id = pk, status = WebApplication1.Utils.Constants.StudentStatus.Suspended });
        }

        public async Task<ServiceResult<object>> ActivateStudentAsync(string pk, CancellationToken ct = default)
        {
            var student = await _context.AccountsCustomusers.Include(u => u.StudentsStudentprofile).FirstOrDefaultAsync(u => u.StudentsStudentprofile != null && (u.Username == pk || u.Id.ToString() == pk || u.StudentsStudentprofile.StudentId == pk), ct);
            if (student == null) return ServiceResult<object>.NotFound("Student not found");

            student.StudentsStudentprofile!.Status = WebApplication1.Utils.Constants.StudentStatus.Live;
            student.StudentsStudentprofile.SuspendedAt = null;
            student.StudentsStudentprofile.SuspensionReason = null;
            await _context.SaveChangesAsync(ct);

            if (!string.IsNullOrWhiteSpace(student.Email))
            {
                var email = student.Email!;
                _ = Task.Run(() => _emailService.SendActivatedEmailAsync(email));
            }

            return ServiceResult<object>.Ok(new { student_id = pk, status = WebApplication1.Utils.Constants.StudentStatus.Live });
        }
        
        public async Task<ServiceResult<object>> GetStudentRelatedDataAsync(string pk, string kind, CancellationToken ct = default)
        {
            var student = await _context.AccountsCustomusers
                .AsNoTracking()
                .Include(u => u.StudentsStudentprofile)
                .FirstOrDefaultAsync(u => u.StudentsStudentprofile != null && 
                    (u.Username == pk || u.Id.ToString() == pk || u.StudentsStudentprofile.StudentId == pk), ct);
            
            if (student == null) return ServiceResult<object>.NotFound("Student not found");

            if (kind == "attendance")
            {
                var data = await _context.AttendanceAttendances.Where(a => a.StudentId == student.Id).OrderByDescending(a => a.Date).Take(30).Select(a => new {
                    id = a.Id, date = a.Date, is_present = a.IsPresent, time_in = a.TimeIn, time_out = a.TimeOut, total_hours = a.TotalHours
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
            var student = await _context.AccountsCustomusers.Include(u => u.StudentsStudentprofile).FirstOrDefaultAsync(u => u.StudentsStudentprofile != null && (u.Username == pk || u.Id.ToString() == pk || u.StudentsStudentprofile.StudentId == pk), ct);
            if (student == null) return ServiceResult<object>.NotFound("Student not found");

            if (photo == null || photo.Length == 0)
                return ServiceResult<object>.Fail("No file uploaded");

            var uploadsFolder = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot", "media", "student_photos");
            if (!System.IO.Directory.Exists(uploadsFolder))
                System.IO.Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + photo.FileName;
            var filePath = System.IO.Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
            {
                await photo.CopyToAsync(fileStream, ct);
            }

            var relativePath = "student_photos/" + uniqueFileName;
            student.StudentsStudentprofile!.ProfilePhoto = relativePath;
            student.StudentsStudentprofile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            return ServiceResult<object>.Ok(new { 
                profile_photo = $"{scheme}://{host}/media/{relativePath}" 
            });
        }

        public async Task<ServiceResult<object>> ExportStudentsAsync(CancellationToken ct = default)
        {
            var students = await _context.StudentsStudentprofiles
                .AsNoTracking()
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
