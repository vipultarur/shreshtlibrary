using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public class AdminNotificationService : IAdminNotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public AdminNotificationService(ApplicationDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<ServiceResult<object>> GetNotificationTemplatesAsync(CancellationToken ct = default)
        {
            var templates = new[]
            {
                new { id = "1", title = "Payment Reminder", body = "Your membership payment is due soon. Please renew to continue using the library." },
                new { id = "2", title = "Holiday Notice", body = "The library will be closed tomorrow for public holiday." },
                new { id = "3", title = "Welcome!", body = "Welcome to Shresht Library. Let's start your study journey." }
            };
            return await Task.FromResult(ServiceResult<object>.Ok(templates));
        }

        public async Task<ServiceResult<object>> GetScheduledNotificationsAsync(CancellationToken ct = default)
        {
            var dbData = await _context.NotificationsNotifications
                .Where(n => n.ScheduledAt != null && n.SentAt == null)
                .OrderBy(n => n.ScheduledAt)
                .ToListAsync(ct);
            
            var data = dbData.Select(n => new Dictionary<string, object?> {
                { "id", n.Id },
                { "title", n.Title },
                { "message", n.Body },
                { "body", n.Body },
                { "type", n.Type },
                { "notification_type", n.Type },
                { "target_group", n.TargetGroup },
                { "audience", n.Audience },
                { "total_recipients", n.TotalRecipients },
                { "scheduled_at", n.ScheduledAt },
                { "created_at", n.CreatedAt }
            }).ToList();
            
            return ServiceResult<object>.Ok(data);
        }

        public async Task<ServiceResult<object>> CancelScheduledNotificationAsync(long pk, CancellationToken ct = default)
        {
            var notification = await _context.NotificationsNotifications.FindAsync(new object[] { pk }, ct);
            if (notification != null && notification.SentAt == null)
            {
                var sn = _context.NotificationsStudentnotifications.Where(s => s.NotificationId == notification.Id);
                _context.NotificationsStudentnotifications.RemoveRange(sn);
                var ni = _context.NotificationsNotificationimages.Where(i => i.NotificationId == notification.Id);
                _context.NotificationsNotificationimages.RemoveRange(ni);
                
                _context.NotificationsNotifications.Remove(notification);
                await _context.SaveChangesAsync(ct);
            }
            return ServiceResult<object>.Ok(new { message = "Cancelled successfully." });
        }

        public async Task<ServiceResult<object>> ProcessNotificationAsync(NotificationPayloadDto dto, bool isSchedule, CancellationToken ct = default)
        {
            var notification = new NotificationsNotification
            {
                Title = dto.Title ?? "Notification",
                Body = dto.Body ?? "",
                Type = dto.Type ?? "GENERAL",
                TargetGroup = dto.TargetGroup ?? "all",
                Target = dto.Target ?? "ALL",
                Audience = dto.Audience ?? "all",
                DisplayMode = dto.DisplayMode ?? "persistent",
                Layout = dto.Layout ?? "text_only",
                Subtitle = dto.Subtitle ?? "",
                Description = dto.Description ?? "",
                LinkButtonText = dto.LinkButtonText ?? "",
                LinkUrl = dto.LinkUrl ?? "",
                SendPush = dto.SendPush ?? true,
                SendEmail = dto.SendEmail ?? false,
                SendSms = dto.SendSms ?? false,
                CreatedAt = DateTime.UtcNow,
                FailureCount = 0,
                SuccessCount = 0,
                TotalRecipients = 0
            };

            if (isSchedule && dto.ScheduledAt.HasValue)
            {
                notification.ScheduledAt = dto.ScheduledAt.Value;
            }

            if (dto.ExpiresAt.HasValue) notification.ExpiresAt = dto.ExpiresAt.Value;
            if (dto.EventDate.HasValue) notification.EventDate = DateOnly.FromDateTime(dto.EventDate.Value);
            if (dto.RecurringTime.HasValue) notification.RecurringTime = dto.RecurringTime.Value;

            if (dto.BackgroundImage != null)
            {
                var mediaPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "..", "shreshtlibrary", "media"));
                var fileName = $"notifications/bg_{Guid.NewGuid()}{System.IO.Path.GetExtension(dto.BackgroundImage.FileName)}";
                var uploadPath = System.IO.Path.Combine(mediaPath, fileName);
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(uploadPath)!);
                using var stream = new System.IO.FileStream(uploadPath, System.IO.FileMode.Create);
                await dto.BackgroundImage.CopyToAsync(stream, ct);
                notification.BackgroundImage = fileName;
            }

            _context.NotificationsNotifications.Add(notification);
            await _context.SaveChangesAsync(ct);

            // Handle gallery images for half_image / full_image layouts
            if (dto.Images != null && dto.Images.Count > 0)
            {
                var mediaPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "..", "shreshtlibrary", "media"));
                int sortOrder = 0;
                foreach (var img in dto.Images)
                {
                    var fileName = $"notifications/img_{Guid.NewGuid()}{System.IO.Path.GetExtension(img.FileName)}";
                    var uploadPath = System.IO.Path.Combine(mediaPath, fileName);
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(uploadPath)!);
                    using var imgStream = new System.IO.FileStream(uploadPath, System.IO.FileMode.Create);
                    await img.CopyToAsync(imgStream, ct);

                    _context.NotificationsNotificationimages.Add(new NotificationsNotificationimage
                    {
                        NotificationId = notification.Id,
                        Image = fileName,
                        SortOrder = sortOrder++
                    });
                }
                await _context.SaveChangesAsync(ct);
            }

            var studentIds = new List<long>();
            var query = _context.StudentsStudentprofiles.AsQueryable();

            if (dto.Audience == "premium")
            {
                var activeStudentIds = await _context.MembershipsMemberships
                    .Where(m => m.EndDate >= DateOnly.FromDateTime(DateTime.UtcNow) && m.Status == "active")
                    .Select(m => m.StudentId)
                    .ToListAsync(ct);
                query = query.Where(p => p.Status == "LIVE" && activeStudentIds.Contains(p.UserId));
            }
            else if (dto.Audience == "expired")
            {
                var activeStudentIds = await _context.MembershipsMemberships
                    .Where(m => m.EndDate >= DateOnly.FromDateTime(DateTime.UtcNow) && m.Status == "active")
                    .Select(m => m.StudentId)
                    .ToListAsync(ct);
                query = query.Where(p => !activeStudentIds.Contains(p.UserId));
            }
            else if (dto.Audience == "selected" && !string.IsNullOrEmpty(dto.SelectedStudents))
            {
                var ids = dto.SelectedStudents.Split(',').Select(s => long.TryParse(s, out var id) ? id : 0).Where(i => i > 0).ToList();
                query = query.Where(p => ids.Contains(p.UserId));
            }

            var users = await query.Select(p => p.UserId).ToListAsync(ct);
            notification.TotalRecipients = users.Count;

            var studentNotifications = new List<NotificationsStudentnotification>();
            foreach(var uid in users)
            {
                studentNotifications.Add(new NotificationsStudentnotification
                {
                    NotificationId = notification.Id,
                    StudentId = uid,
                    IsRead = false,
                    PushDelivered = false,
                    EmailDelivered = false,
                    SmsDelivered = false
                });
            }
            _context.NotificationsStudentnotifications.AddRange(studentNotifications);
            await _context.SaveChangesAsync(ct);

            if (!isSchedule)
            {
                var tokens = await _context.NotificationsDevicetokens
                        .Where(dt => users.Contains(dt.StudentId))
                        .Select(dt => dt.Token)
                        .ToListAsync(ct);
                
                int successCount = 0;
                if (notification.SendPush && tokens.Count > 0 && _notificationService != null)
                {
                    var data = new Dictionary<string, string>
                    {
                        { "notification_id", notification.Id.ToString() },
                        { "type", notification.Type },
                        { "link_url", notification.LinkUrl ?? "" }
                    };
                    successCount = await _notificationService.SendMulticastPushNotificationAsync(tokens, notification.Title, notification.Body, data);
                }

                notification.SentAt = DateTime.UtcNow;
                notification.SuccessCount = successCount;
                notification.FailureCount = tokens.Count - successCount;

                foreach (var sn in studentNotifications)
                {
                    sn.DeliveredAt = DateTime.UtcNow;
                    sn.PushDelivered = notification.SendPush;
                }
                
                await _context.SaveChangesAsync(ct);
            }

            return ServiceResult<object>.Ok(notification);
        }

        public async Task<ServiceResult<object>> GetNotificationsListAsync(int page, int pageSize, CancellationToken ct = default)
        {
            pageSize = System.Math.Clamp(pageSize, 1, 100);
            
            var query = _context.NotificationsNotifications.Where(n => n.ScheduledAt == null || n.SentAt != null).OrderByDescending(n => n.CreatedAt);
            var totalCount = await query.CountAsync(ct);
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            
            var dbData = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
            
            var data = dbData.Select(n => new Dictionary<string, object?> {
                { "id", n.Id },
                { "title", n.Title },
                { "body", n.Body },
                { "type", n.Type },
                { "target", n.TargetGroup },
                { "target_group", n.TargetGroup },
                { "created_at", n.CreatedAt },
                { "sent_at", n.SentAt },
                { "scheduled_at", n.ScheduledAt },
                { "success_count", n.SuccessCount },
                { "failure_count", n.FailureCount },
                { "total_recipients", n.TotalRecipients },
                { "send_push", n.SendPush },
                { "send_email", n.SendEmail },
                { "send_sms", n.SendSms }
            }).ToList();
            
            return ServiceResult<object>.Ok(new {
                data = data,
                count = totalCount,
                total_pages = totalPages == 0 ? 1 : totalPages,
                current_page = page
            });
        }

        public async Task<ServiceResult<object>> GetNotificationDetailAsync(long pk, CancellationToken ct = default)
        {
            var notification = await _context.NotificationsNotifications.FindAsync(new object[] { pk }, ct);
            if (notification == null) return ServiceResult<object>.NotFound("Notification not found");
            return ServiceResult<object>.Ok(notification);
        }

        public async Task<ServiceResult<object>> GetNotificationRecipientsAsync(long pk, CancellationToken ct = default)
        {
            var recipients = await _context.NotificationsStudentnotifications
                .Where(sn => sn.NotificationId == pk)
                .Include(sn => sn.Student)
                .Select(sn => new {
                    id = sn.Id,
                    student_id = sn.StudentId,
                    student_name = sn.Student != null ? $"{sn.Student.FirstName} {sn.Student.LastName}" : "Unknown",
                    is_read = sn.IsRead,
                    read_at = sn.ReadAt,
                    delivered_at = sn.DeliveredAt
                }).ToListAsync(ct);

            return ServiceResult<object>.Ok(recipients);
        }

        public async Task<ServiceResult<object>> GetInboxNotificationsAsync(CancellationToken ct = default)
        {
            var dbNotifications = await _context.NotificationsAdmininboxnotifications
                .Include(n => n.Student)
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
                .AsNoTracking()
                .ToListAsync(ct);

            var notifications = dbNotifications.Select(n => new {
                id = n.Id,
                title = n.Title,
                message = n.Message,
                type = n.Type,
                is_read = n.IsRead,
                related_id = n.RelatedId,
                student_id = n.StudentId,
                student_name = n.Student != null ? $"{n.Student.FirstName} {n.Student.LastName}" : null,
                student_avatar = (string?)null,
                created_at = n.CreatedAt
            }).ToList();

            return ServiceResult<object>.Ok(notifications);
        }

        public async Task<ServiceResult<bool>> MarkInboxActionAsync(long pk, string action, CancellationToken ct = default)
        {
            var notification = await _context.NotificationsAdmininboxnotifications.FindAsync(new object[] { pk }, ct);
            if (notification == null) return ServiceResult<bool>.NotFound("Notification not found");

            if (action == "read")
            {
                notification.IsRead = true;
            }
            else if (action == "unread")
            {
                notification.IsRead = false;
            }

            await _context.SaveChangesAsync(ct);
            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<bool>> DeleteInboxNotificationAsync(long pk, CancellationToken ct = default)
        {
            var notification = await _context.NotificationsAdmininboxnotifications.FindAsync(new object[] { pk }, ct);
            if (notification == null) return ServiceResult<bool>.NotFound("Notification not found");

            _context.NotificationsAdmininboxnotifications.Remove(notification);
            await _context.SaveChangesAsync(ct);
            return ServiceResult<bool>.Ok(true);
        }
    }
}
