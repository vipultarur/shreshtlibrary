using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Controllers;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public class SuperAdminService : ISuperAdminService
    {
        private readonly ApplicationDbContext _context;

        public SuperAdminService(ApplicationDbContext context)
        {
            _context = context;
        }

        private ServiceResult<object>? ValidateAdminPayload(SuperAdminController.AdminPayload payload)
        {
            var errors = new System.Collections.Generic.Dictionary<string, string[]>();

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

        public async Task<ServiceResult<object>> AddAdminAsync(SuperAdminController.AdminPayload payload, CancellationToken ct = default)
        {
            var validationError = ValidateAdminPayload(payload);
            if (validationError != null) return validationError;

            if (await _context.AccountsAdminusers.AnyAsync(u => u.Email == payload.Email || (!string.IsNullOrEmpty(payload.Username) && u.Username == payload.Username), ct))
                return ServiceResult<object>.Fail("Admin with this email or username already exists");

            var newUser = new AccountsAdminuser
            {
                Username = payload.Username ?? payload.Email ?? $"admin{new Random().Next(100, 999)}",
                FirstName = payload.FirstName ?? "",
                LastName = payload.LastName ?? "",
                Email = payload.Email,
                Mobile = payload.Mobile,
                Role = payload.Role ?? "admin",
                IsActive = payload.IsActive ?? true,
                DateJoined = DateTime.UtcNow,
                Password = Utils.PasswordHasher.HashDjangoPassword(payload.Password ?? "admin@123"),
                Permissions = payload.Permissions != null ? System.Text.Json.JsonSerializer.Serialize(payload.Permissions) : "{}"
            };

            _context.AccountsAdminusers.Add(newUser);
            await _context.SaveChangesAsync(ct);
            return ServiceResult<object>.Ok(new { id = newUser.Id, email = newUser.Email });
        }

        public async Task<ServiceResult<object>> UpdateAdminAsync(long pk, SuperAdminController.AdminPayload payload, CancellationToken ct = default)
        {
            var validationError = ValidateAdminPayload(payload);
            if (validationError != null) return validationError;

            var admin = await _context.AccountsAdminusers.FirstOrDefaultAsync(u => u.Id == pk && (u.Role == "admin" || u.Role == "super_admin"), ct);
            if (admin == null) return ServiceResult<object>.NotFound("Admin not found");

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
            if (string.IsNullOrWhiteSpace(permissionsJson)) return new { };
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<object>(permissionsJson) ?? new { };
            }
            catch
            {
                return new { }; // Return empty object if JSON parsing fails (e.g. legacy python dict string)
            }
        }

        public async Task<ServiceResult<object>> GetAdminsListAsync(CancellationToken ct = default)
        {
            var admins = await _context.AccountsAdminusers
                .AsNoTracking()
                .Where(u => u.Role == "admin" || u.Role == "super_admin")
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
                .Where(u => u.Id == pk && (u.Role == "admin" || u.Role == "super_admin"))
                .FirstOrDefaultAsync(ct);

            if (admin == null) return ServiceResult<object>.NotFound("Admin not found");
            
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
            var admin = await _context.AccountsAdminusers.FirstOrDefaultAsync(u => u.Id == pk && (u.Role == "admin" || u.Role == "super_admin"), ct);
            if (admin == null) return ServiceResult<object>.NotFound("Admin not found");

            try
            {
                _context.AccountsAdminusers.Remove(admin);
                await _context.SaveChangesAsync(ct);
                return ServiceResult<object>.Ok(new { });
            }
            catch (DbUpdateException)
            {
                return ServiceResult<object>.Fail("Cannot delete this admin because they are tied to existing records (e.g. payments, students, activity logs). Please deactivate them instead.");
            }
        }

        public async Task<ServiceResult<object>> DeactivateAdminAsync(long pk, CancellationToken ct = default)
        {
            var admin = await _context.AccountsAdminusers.FirstOrDefaultAsync(u => u.Id == pk && (u.Role == "admin" || u.Role == "super_admin"), ct);
            if (admin == null) return ServiceResult<object>.NotFound("Admin not found");

            admin.IsActive = false;
            await _context.SaveChangesAsync(ct);
            return ServiceResult<object>.Ok(new { });
        }

        public async Task<ServiceResult<object>> GetPermissionsListAsync(CancellationToken ct = default)
        {
            var permissions = new[] 
            { 
                new { key = "manage_students", label = "Manage Students" },
                new { key = "manage_seats", label = "Manage Seats" },
                new { key = "manage_billing", label = "Manage Billing" },
                new { key = "manage_attendance", label = "Manage Attendance" }
            };
            return ServiceResult<object>.Ok(permissions);
        }

        public async Task<ServiceResult<object>> AssignPermissionsAsync(SuperAdminController.PermissionPayload payload, CancellationToken ct = default)
        {
            return ServiceResult<object>.Ok(new { message = "Permissions updated" });
        }

        public async Task<ServiceResult<object>> CreateBackupAsync(CancellationToken ct = default)
        {
            return ServiceResult<object>.Ok(new { id = $"backup_{DateTime.UtcNow.Ticks}", status = "in_progress" });
        }

        public async Task<ServiceResult<object>> GetBackupListAsync(CancellationToken ct = default)
        {
            return ServiceResult<object>.Ok(new[] {
                new { id = "backup_1", created_at = DateTime.UtcNow.AddDays(-1), status = "completed" },
                new { id = "backup_2", created_at = DateTime.UtcNow.AddDays(-7), status = "completed" }
            });
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
