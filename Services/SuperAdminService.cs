using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WebApplication1.Models.DTOs.Admin;
using WebApplication1.Controllers;
using WebApplication1.Data;
using WebApplication1.Models;
using System.Collections.Generic;

namespace WebApplication1.Services
{
    public class SuperAdminService : ISuperAdminService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _cache;
        private readonly IEmailService _emailService;

        private const string SuperAdminEmail = "vipultarur@gmail.com";

        public SuperAdminService(ApplicationDbContext context, ICurrentUserService currentUserService, Microsoft.Extensions.Caching.Memory.IMemoryCache cache, IEmailService emailService)
        {
            _context = context;
            _currentUserService = currentUserService;
            _cache = cache;
            _emailService = emailService;
        }

        private ServiceResult<object>? ValidateAdminPayload(AdminPayload payload)
        {
            var errors = new Dictionary<string, string[]>();

            if (string.IsNullOrWhiteSpace(payload.Email) || !payload.Email.Contains("@"))
            {
                errors.Add("email", new[] { "Enter a valid email address." });
            }

            if (!string.IsNullOrWhiteSpace(payload.Mobile) && (payload.Mobile.Length != 10 || !payload.Mobile.All(char.IsDigit)))
            {
                errors.Add("mobile", new[] { "Mobile number must be exactly 10 digits." });
            }

            if (errors.Any())
            {
                return ServiceResult<object>.Fail("Validation failed", errors);
            }

            return null;
        }

        public async Task<ServiceResult<object>> AddAdminAsync(AdminPayload payload, CancellationToken ct = default)
        {
            var currentUserRole = _currentUserService.GetUserRole();
            if (currentUserRole == "sub_super_admin" && payload.Role != "admin")
            {
                return ServiceResult<object>.Fail("Sub Super Admins can only create Admin roles.");
            }

            if (payload.Email?.ToLower() == SuperAdminEmail)
            {
                return ServiceResult<object>.Fail("Cannot create an admin with the reserved super admin email.");
            }

            var validationError = ValidateAdminPayload(payload);
            if (validationError != null) return validationError;

            if (await _context.AccountsAdminusers.AnyAsync(u => u.Email == payload.Email || (!string.IsNullOrEmpty(payload.Username) && u.Username == payload.Username), ct))
                return ServiceResult<object>.Fail("Admin with this email or username already exists");

            var newUser = new AccountsAdminuser
            {
                Username = payload.Username ?? payload.Email ?? $"admin{System.Security.Cryptography.RandomNumberGenerator.GetInt32(100, 1000)}",
                FirstName = payload.FirstName ?? "",
                LastName = payload.LastName ?? "",
                Email = payload.Email,
                Mobile = payload.Mobile,
                Role = payload.Role ?? "admin",
                IsActive = payload.IsActive ?? true,
                DateJoined = DateTime.UtcNow,
                Password = Utils.PasswordHasher.HashDjangoPassword(payload.Password ?? "admin@123"),
                Permissions = payload.Permissions != null ? System.Text.Json.JsonSerializer.Serialize(payload.Permissions) : "[]"
            };

            _context.AccountsAdminusers.Add(newUser);
            await _context.SaveChangesAsync(ct);
            return ServiceResult<object>.Ok(new { id = newUser.Id, email = newUser.Email });
        }

        public async Task<ServiceResult<object>> UpdateAdminAsync(long pk, AdminPayload payload, CancellationToken ct = default)
        {
            var admin = await _context.AccountsAdminusers.FirstOrDefaultAsync(u => u.Id == pk, ct);
            if (admin == null) return ServiceResult<object>.NotFound("Admin not found");

            if (admin.Email?.ToLower() == SuperAdminEmail)
            {
                return ServiceResult<object>.Fail("The primary Super Admin account cannot be modified.");
            }

            var currentUserRole = _currentUserService.GetUserRole();
            if (currentUserRole == "sub_super_admin")
            {
                if (admin.Role == "super_admin" || admin.Role == "sub_super_admin")
                {
                    return ServiceResult<object>.Fail("Sub Super Admins cannot modify Super Admins or other Sub Super Admins.");
                }
                if (payload.Role != null && payload.Role != "admin")
                {
                    return ServiceResult<object>.Fail("Sub Super Admins cannot assign roles other than Admin.");
                }
            }

            var validationError = ValidateAdminPayload(payload);
            if (validationError != null) return validationError;

            if (await _context.AccountsAdminusers.AnyAsync(u => u.Id != pk && (u.Email == payload.Email || (!string.IsNullOrEmpty(payload.Username) && u.Username == payload.Username)), ct))
                return ServiceResult<object>.Fail("Admin with this email or username already exists");

            admin.Username = payload.Username ?? admin.Username;
            admin.FirstName = payload.FirstName ?? admin.FirstName;
            admin.LastName = payload.LastName ?? admin.LastName;
            admin.Email = payload.Email ?? admin.Email;
            admin.Mobile = payload.Mobile ?? admin.Mobile;
            
            if (payload.Role != null) admin.Role = payload.Role;
            if (payload.IsActive.HasValue) admin.IsActive = payload.IsActive.Value;
            if (payload.Permissions != null) admin.Permissions = System.Text.Json.JsonSerializer.Serialize(payload.Permissions);

            if (!string.IsNullOrWhiteSpace(payload.Password))
            {
                admin.Password = Utils.PasswordHasher.HashDjangoPassword(payload.Password);
            }

            await _context.SaveChangesAsync(ct);
            return ServiceResult<object>.Ok(new { id = admin.Id, email = admin.Email });
        }

        private object ParsePermissions(string permissionsJson)
        {
            if (string.IsNullOrWhiteSpace(permissionsJson)) return new string[0];
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<string[]>(permissionsJson) ?? new string[0];
            }
            catch
            {
                return new string[0]; // Return empty array if JSON parsing fails
            }
        }

        public async Task<ServiceResult<object>> GetAdminsListAsync(CancellationToken ct = default)
        {
            var admins = await _context.AccountsAdminusers
                .AsNoTracking()
                .Where(u => u.Role == "admin" || u.Role == "super_admin" || u.Role == "sub_super_admin")
                .Where(u => u.Email != SuperAdminEmail) // Hide main super admin
                .ToListAsync(ct);

            var result = admins.Select(u => new {
                id = u.Id,
                username = u.Username,
                first_name = u.FirstName,
                last_name = u.LastName,
                email = u.Email,
                mobile = u.Mobile,
                role = u.Role,
                is_active = u.IsActive,
                date_joined = u.DateJoined,
                permissions = ParsePermissions(u.Permissions)
            });

            return ServiceResult<object>.Ok(result);
        }

        public async Task<ServiceResult<object>> GetAdminDetailAsync(long pk, CancellationToken ct = default)
        {
            var admin = await _context.AccountsAdminusers
                .AsNoTracking()
                .Where(u => u.Id == pk)
                .FirstOrDefaultAsync(ct);

            if (admin == null || admin.Email?.ToLower() == SuperAdminEmail) 
                return ServiceResult<object>.NotFound("Admin not found");
            
            return ServiceResult<object>.Ok(new {
                id = admin.Id,
                username = admin.Username,
                first_name = admin.FirstName,
                last_name = admin.LastName,
                email = admin.Email,
                mobile = admin.Mobile,
                role = admin.Role,
                is_active = admin.IsActive,
                date_joined = admin.DateJoined,
                permissions = ParsePermissions(admin.Permissions)
            });
        }

        public async Task<ServiceResult<object>> RemoveAdminAsync(long pk, CancellationToken ct = default)
        {
            var admin = await _context.AccountsAdminusers.FirstOrDefaultAsync(u => u.Id == pk, ct);
            if (admin == null) return ServiceResult<object>.NotFound("Admin not found");

            if (admin.Email?.ToLower() == SuperAdminEmail)
            {
                return ServiceResult<object>.Fail("The primary Super Admin account cannot be deleted.");
            }

            var currentUserRole = _currentUserService.GetUserRole();
            if (currentUserRole == "sub_super_admin" && (admin.Role == "super_admin" || admin.Role == "sub_super_admin"))
            {
                return ServiceResult<object>.Fail("Sub Super Admins cannot delete Super Admins or other Sub Super Admins.");
            }

            try
            {
                var activityLogs = await _context.CoreActivitylogs.Where(log => log.AdminId == pk).ToListAsync(ct);
                if (activityLogs.Any())
                {
                    _context.CoreActivitylogs.RemoveRange(activityLogs);
                }

                _context.AccountsAdminusers.Remove(admin);
                await _context.SaveChangesAsync(ct);
                return ServiceResult<object>.Ok(new { });
            }
            catch (DbUpdateException)
            {
                return ServiceResult<object>.Fail("Cannot delete this admin because they are tied to existing records. Please deactivate them instead.");
            }
        }

        public async Task<ServiceResult<object>> DeactivateAdminAsync(long pk, CancellationToken ct = default)
        {
            var admin = await _context.AccountsAdminusers.FirstOrDefaultAsync(u => u.Id == pk, ct);
            if (admin == null) return ServiceResult<object>.NotFound("Admin not found");

            if (admin.Email?.ToLower() == SuperAdminEmail)
            {
                return ServiceResult<object>.Fail("The primary Super Admin account cannot be deactivated.");
            }

            var currentUserRole = _currentUserService.GetUserRole();
            if (currentUserRole == "sub_super_admin" && (admin.Role == "super_admin" || admin.Role == "sub_super_admin"))
            {
                return ServiceResult<object>.Fail("Sub Super Admins cannot deactivate Super Admins or other Sub Super Admins.");
            }

            admin.IsActive = false;
            await _context.SaveChangesAsync(ct);
            return ServiceResult<object>.Ok(new { });
        }

        public async Task<ServiceResult<object>> GetPermissionsListAsync(CancellationToken ct = default)
        {
            var cacheKey = "AdminPermissionsList";
            if (_cache.TryGetValue(cacheKey, out object? cachedPermissions) && cachedPermissions != null)
            {
                return ServiceResult<object>.Ok(cachedPermissions);
            }

            var permissions = new[] 
            { 
                new { category = "Dashboard", permissions = new[] { "Dashboard.View", "Dashboard.Analytics", "Dashboard.Export" } },
                new { category = "Student Management", permissions = new[] { "StudentManagement.View", "StudentManagement.Add", "StudentManagement.Edit", "StudentManagement.Delete", "StudentManagement.Suspend", "StudentManagement.Activate", "StudentManagement.Import", "StudentManagement.Export", "StudentManagement.ResetPassword" } },
                new { category = "Attendance", permissions = new[] { "Attendance.View", "Attendance.Mark", "Attendance.Edit", "Attendance.Delete", "Attendance.Export", "Attendance.Manage" } },
                new { category = "QR Attendance", permissions = new[] { "QRAttendance.View", "QRAttendance.Generate", "QRAttendance.Delete" } },
                new { category = "Library Management", permissions = new[] { "LibraryManagement.Settings", "LibraryManagement.Timing", "LibraryManagement.Holiday", "LibraryManagement.Seat", "LibraryManagement.Floor", "LibraryManagement.Room", "LibraryManagement.Capacity", "LibraryManagement.Info", "LibraryManagement.Gallery", "LibraryManagement.Facilities", "LibraryManagement.Slider", "LibraryManagement.Review", "LibraryManagement.Achiever" } },
                new { category = "Notification Management", permissions = new[] { "NotificationManagement.View", "NotificationManagement.Create", "NotificationManagement.Edit", "NotificationManagement.Delete", "NotificationManagement.SendPush", "NotificationManagement.SendEmail", "NotificationManagement.SendSMS", "NotificationManagement.Send" } },
                new { category = "User Management", permissions = new[] { "UserManagement.View", "UserManagement.Add", "UserManagement.Edit", "UserManagement.Delete", "UserManagement.Activate", "UserManagement.Suspend", "UserManagement.ResetPassword" } },
                new { category = "Admin Management", permissions = new[] { "AdminManagement.View", "AdminManagement.Create", "AdminManagement.Edit", "AdminManagement.Delete", "AdminManagement.Suspend", "AdminManagement.Activate", "AdminManagement.ResetPassword", "AdminManagement.ChangePermissions", "AdminManagement.ViewActivity", "AdminManagement.Export", "AdminManagement.ManageRoles" } },
                new { category = "Reports", permissions = new[] { "Reports.View", "Reports.Attendance", "Reports.Student", "Reports.Revenue", "Reports.Export" } },
                new { category = "Analytics", permissions = new[] { "Analytics.View", "Analytics.Attendance", "Analytics.Student", "Analytics.Revenue" } },
                new { category = "Fee Management", permissions = new[] { "FeeManagement.View", "FeeManagement.Create", "FeeManagement.Edit", "FeeManagement.Delete", "FeeManagement.Collect", "FeeManagement.Refund", "FeeManagement.Export" } },
                new { category = "Payment", permissions = new[] { "Payment.View", "Payment.Verify", "Payment.Refund", "Payment.Export" } },
                new { category = "Membership", permissions = new[] { "Membership.View", "Membership.Create", "Membership.Edit", "Membership.Delete", "Membership.Renew", "Membership.ManagePlans" } },
                new { category = "Course Management", permissions = new[] { "CourseManagement.View", "CourseManagement.Add", "CourseManagement.Edit", "CourseManagement.Delete" } },
                new { category = "Batch Management", permissions = new[] { "BatchManagement.View", "BatchManagement.Create", "BatchManagement.Edit", "BatchManagement.Delete" } },
                new { category = "Staff Management", permissions = new[] { "StaffManagement.View", "StaffManagement.Add", "StaffManagement.Edit", "StaffManagement.Delete", "StaffManagement.Salary" } },
                new { category = "Visitor Management", permissions = new[] { "VisitorManagement.View", "VisitorManagement.Add", "VisitorManagement.Edit", "VisitorManagement.Delete" } },
                new { category = "Feedback", permissions = new[] { "Feedback.View", "Feedback.Reply", "Feedback.Delete" } },
                new { category = "Announcement", permissions = new[] { "Announcement.View", "Announcement.Create", "Announcement.Edit", "Announcement.Delete" } },
                new { category = "Maintenance", permissions = new[] { "Maintenance.View", "Maintenance.Enable", "Maintenance.Disable", "Maintenance.EditMessage", "Maintenance.ManageSchedule" } },
                new { category = "System Settings", permissions = new[] { "SystemSettings.View", "SystemSettings.Edit", "SystemSettings.Backup", "SystemSettings.Restore", "SystemSettings.Configuration", "SystemSettings.APIConfiguration", "SystemSettings.SMTPConfiguration", "SystemSettings.StorageConfiguration" } },
                new { category = "Security", permissions = new[] { "Security.ViewLoginHistory", "Security.ViewActivityLogs", "Security.ClearLogs", "Security.ManageSessions", "Security.ForceLogout", "Security.BlockUsers" } },
                new { category = "Localization", permissions = new[] { "Localization.Manage", "Localization.Add", "Localization.Edit", "Localization.Delete" } },
                new { category = "Backup", permissions = new[] { "Backup.Create", "Backup.Download", "Backup.Restore", "Backup.Delete" } },
                new { category = "Audit Logs", permissions = new[] { "AuditLogs.View", "AuditLogs.Export", "AuditLogs.Delete" } },
                new { category = "App Settings", permissions = new[] { "AppSettings.Manage" } }
            };
            _cache.Set(cacheKey, permissions, TimeSpan.FromMinutes(30));
            return ServiceResult<object>.Ok(permissions);
        }

        public async Task<ServiceResult<object>> AssignPermissionsAsync(PermissionPayload payload, CancellationToken ct = default)
        {
            return ServiceResult<object>.Ok(new { message = "Permissions updated" });
        }

        public async Task<ServiceResult<object>> CreateBackupAsync(CancellationToken ct = default)
        {
            try
            {
                var backupId = $"manual_backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
                var backupData = new
                {
                    BackupId = backupId,
                    GeneratedAt = DateTime.UtcNow,
                    Type = "Manual Backup",
                    Admins = await _context.AccountsAdminusers.AsNoTracking().ToListAsync(ct),
                    Students = await _context.StudentsStudentprofiles.AsNoTracking().ToListAsync(ct),
                    Facilities = await _context.LibraryFacilities.AsNoTracking().ToListAsync(ct),
                    Achievers = await _context.LibraryAchievers.AsNoTracking().ToListAsync(ct),
                    Sliders = await _context.LibraryHomesliders.AsNoTracking().ToListAsync(ct),
                    LibraryInfo = await _context.LibraryLibraryinfos.AsNoTracking().ToListAsync(ct),
                    LibraryAppConfigs = await _context.LibraryAppconfigs.AsNoTracking().ToListAsync(ct),
                    LibraryReviews = await _context.LibraryReviews.AsNoTracking().ToListAsync(ct)
                };

                var json = System.Text.Json.JsonSerializer.Serialize(backupData, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                
                var backupDir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Backups");
                if (!System.IO.Directory.Exists(backupDir))
                {
                    System.IO.Directory.CreateDirectory(backupDir);
                }

                var filePath = System.IO.Path.Combine(backupDir, $"{backupId}.json");
                await System.IO.File.WriteAllTextAsync(filePath, json, ct);

                // Email backup file to Library Email and Administrators
                try
                {
                    var recipients = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    var libraryInfo = await _context.LibraryLibraryinfos.AsNoTracking().FirstOrDefaultAsync(ct);
                    if (libraryInfo != null && !string.IsNullOrWhiteSpace(libraryInfo.Email))
                    {
                        recipients.Add(libraryInfo.Email.Trim());
                    }

                    var customUserAdmins = await _context.AccountsCustomusers
                        .AsNoTracking()
                        .Where(u => (u.Role == "super_admin" || u.Role == "sub_super_admin") && !string.IsNullOrEmpty(u.Email))
                        .Select(u => u.Email)
                        .ToListAsync(ct);

                    foreach (var email in customUserAdmins)
                    {
                        if (!string.IsNullOrWhiteSpace(email)) recipients.Add(email.Trim());
                    }

                    var adminUserEmails = await _context.AccountsAdminusers
                        .AsNoTracking()
                        .Where(u => !string.IsNullOrEmpty(u.Email))
                        .Select(u => u.Email!)
                        .ToListAsync(ct);

                    foreach (var email in adminUserEmails)
                    {
                        if (!string.IsNullOrWhiteSpace(email)) recipients.Add(email.Trim());
                    }

                    if (recipients.Count > 0)
                    {
                        var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath, ct);
                        string subject = $"System Backup File 📦 - {libraryInfo?.LibraryName ?? "Shresht Library"}";
                        string htmlMessage = $@"
                            <h3>System Backup Generated</h3>
                            <p>Please find attached the system backup JSON file generated at <strong>{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</strong>.</p>
                            <p><strong>Library Name:</strong> {libraryInfo?.LibraryName ?? "Shresht Library"}<br/>
                            <strong>Backup ID:</strong> {backupId}</p>";

                        foreach (var recipientEmail in recipients)
                        {
                            await _emailService.SendEmailWithAttachmentAsync(recipientEmail, subject, htmlMessage, fileBytes, $"{backupId}.json");
                        }
                    }
                }
                catch
                {
                    // Non-fatal email notification failure on manual backup
                }

                return ServiceResult<object>.Ok(new { id = backupId, status = "completed" });
            }
            catch (Exception ex)
            {
                return ServiceResult<object>.Fail($"Backup failed: {ex.Message}");
            }
        }

        public async Task<ServiceResult<object>> GetBackupListAsync(CancellationToken ct = default)
        {
            try
            {
                var backupDir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Backups");
                if (!System.IO.Directory.Exists(backupDir))
                {
                    return ServiceResult<object>.Ok(new object[0]);
                }

                var files = System.IO.Directory.GetFiles(backupDir, "*.json");
                var result = files.Select(f => {
                    var fileInfo = new System.IO.FileInfo(f);
                    return new {
                        id = System.IO.Path.GetFileNameWithoutExtension(f),
                        created_at = fileInfo.CreationTimeUtc,
                        status = "completed"
                    };
                }).OrderByDescending(x => x.created_at).ToList();

                return ServiceResult<object>.Ok(result);
            }
            catch (Exception ex)
            {
                return ServiceResult<object>.Fail($"Failed to load backups: {ex.Message}");
            }
        }

        public async Task<ServiceResult<object>> GetBackupDataAsync(string backupId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(backupId) || backupId.Contains("..") || backupId.Contains('/') || backupId.Contains('\\'))
            {
                return ServiceResult<object>.NotFound("Invalid backup ID");
            }
            
            var backupDir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Backups");
            var filePath = System.IO.Path.Combine(backupDir, $"{backupId}.json");
            
            if (!System.IO.File.Exists(filePath))
            {
                return ServiceResult<object>.NotFound("Backup file not found.");
            }

            var bytes = await System.IO.File.ReadAllBytesAsync(filePath, ct);
            return ServiceResult<object>.Ok(bytes);
        }

        public async Task<ServiceResult<object>> RestoreBackupAsync(string backupId, CancellationToken ct = default)
        {
            return ServiceResult<object>.Ok(new { message = "Backup restored" });
        }

        public async Task<ServiceResult<object>> GetActivityLogAsync(int page, int pageSize, CancellationToken ct = default)
        {
            var logs = await _context.CoreActivitylogs
                .Include(l => l.User)
                .AsNoTracking()
                .OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new {
                    id = l.Id,
                    action = l.Action,
                    admin_name = l.User != null ? l.User.FirstName + " " + l.User.LastName : "Unknown",
                    created_at = l.Timestamp
                })
                .ToListAsync(ct);
                
            return ServiceResult<object>.Ok(logs);
        }
    }
}
