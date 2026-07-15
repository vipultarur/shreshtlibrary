using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public class AdminNotificationService : IAdminNotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly WhatsAppNotificationService _whatsAppService;
        private readonly ILogger<AdminNotificationService> _logger;
        private readonly ICloudinaryService _cloudinary;

        public AdminNotificationService(ApplicationDbContext context, INotificationService notificationService, WhatsAppNotificationService whatsAppService, ILogger<AdminNotificationService> logger, ICloudinaryService cloudinary)
        {
            _context = context;
            _notificationService = notificationService;
            _whatsAppService = whatsAppService;
            _logger = logger;
            _cloudinary = cloudinary;
        }

        public async Task<ServiceResult<object>> GetNotificationTemplatesAsync(CancellationToken ct = default)
        {
            var templates = await _context.NotificationsNotifications
                .Where(n => !string.IsNullOrEmpty(n.Title) && !string.IsNullOrEmpty(n.Body))
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new { title = n.Title, body = n.Body })
                .Distinct()
                .Take(10)
                .ToListAsync(ct);

            var result = templates.Select((t, i) => new {
                id = (i + 1).ToString(),
                title = t.title,
                body = t.body
            }).ToArray();

            return ServiceResult<object>.Ok(result);
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
                notification.ScheduledAt = dto.ScheduledAt.Value.ToUniversalTime();
            }

            if (dto.ExpiresAt.HasValue) notification.ExpiresAt = dto.ExpiresAt.Value.ToUniversalTime();
            if (dto.EventDate.HasValue) notification.EventDate = DateOnly.FromDateTime(dto.EventDate.Value);
            if (dto.RecurringTime.HasValue) notification.RecurringTime = dto.RecurringTime.Value;

            if (dto.BackgroundImage != null)
            {
                var cloudinaryUrl = await _cloudinary.UploadImageAsync(dto.BackgroundImage, "notifications");
                if (!string.IsNullOrEmpty(cloudinaryUrl))
                {
                    notification.BackgroundImage = cloudinaryUrl;
                }
                else
                {
                    return ServiceResult<object>.Fail("Failed to upload background image to Cloudinary.");
                }
            }

            _context.NotificationsNotifications.Add(notification);
            await _context.SaveChangesAsync(ct);

            if (dto.Images != null && dto.Images.Count > 0)
            {
                int sortOrder = 0;
                foreach (var img in dto.Images)
                {
                    var cloudinaryUrl = await _cloudinary.UploadImageAsync(img, "notifications");
                    string imageVal;
                    if (!string.IsNullOrEmpty(cloudinaryUrl))
                    {
                        imageVal = cloudinaryUrl;
                    }
                    else
                    {
                        return ServiceResult<object>.Fail("Failed to upload notification gallery image to Cloudinary.");
                    }

                    _context.NotificationsNotificationimages.Add(new NotificationsNotificationimage
                    {
                        NotificationId = notification.Id,
                        Image = imageVal,
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
            else if (dto.Audience == "free")
            {
                var activeStudentIds = await _context.MembershipsMemberships
                    .Where(m => m.EndDate >= DateOnly.FromDateTime(DateTime.UtcNow) && m.Status == "active")
                    .Select(m => m.StudentId)
                    .ToListAsync(ct);
                query = query.Where(p => p.Status == "LIVE" && !activeStudentIds.Contains(p.UserId));
            }
            else if (dto.Audience == "expired")
            {
                var activeStudentIds = await _context.MembershipsMemberships
                    .Where(m => m.EndDate >= DateOnly.FromDateTime(DateTime.UtcNow) && m.Status == "active")
                    .Select(m => m.StudentId)
                    .ToListAsync(ct);
                var expiredStudentIds = await _context.MembershipsMemberships
                    .Where(m => m.EndDate < DateOnly.FromDateTime(DateTime.UtcNow))
                    .Select(m => m.StudentId)
                    .ToListAsync(ct);
                query = query.Where(p => p.Status == "LIVE" && !activeStudentIds.Contains(p.UserId) && expiredStudentIds.Contains(p.UserId));
            }
            else if (dto.Audience == "pending")
            {
                query = query.Where(p => p.Status == "PENDING");
            }
            else if (dto.Audience == "suspended")
            {
                query = query.Where(p => p.Status == "SUSPENDED");
            }
            else if (dto.Audience == "new")
            {
                var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
                query = query.Where(p => p.Status == "LIVE" && p.CreatedAt >= sevenDaysAgo);
            }
            else if (dto.Audience == "selected")
            {
                if (!string.IsNullOrEmpty(dto.SelectedStudents))
                {
                    var ids = dto.SelectedStudents.Split(',').Select(s => long.TryParse(s, out var id) ? id : 0).Where(i => i > 0).ToList();
                    query = query.Where(p => ids.Contains(p.UserId));
                }
                else
                {
                    query = query.Where(p => false);
                }
            }
            else // "all" or default
            {
                query = query.Where(p => p.Status == "LIVE");
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
                _logger.LogInformation("[FCM DEBUG] SendPush={SendPush}, TokenCount={TokenCount}, ServiceNull={ServiceNull}",
                    notification.SendPush, tokens.Count, _notificationService == null);

                if (notification.SendPush && tokens.Count > 0 && _notificationService != null)
                {
                    var data = new Dictionary<string, string>
                    {
                        { "notification_id", notification.Id.ToString() },
                        { "type", notification.Type ?? "GENERAL" },
                        { "layout", notification.Layout ?? "text_only" },
                        { "display_mode", notification.DisplayMode ?? "persistent" },
                        { "title", notification.Title ?? "" },
                        { "body", notification.Body ?? "" },
                        { "subtitle", notification.Subtitle ?? "" },
                        { "description", notification.Description ?? "" },
                        { "link_url", notification.LinkUrl ?? "" },
                        { "link_button_text", notification.LinkButtonText ?? "" }
                    };

                    var baseUrl = Environment.GetEnvironmentVariable("BASE_URL") ?? "https://shreshtlibrary.onrender.com";

                    if (!string.IsNullOrEmpty(notification.BackgroundImage))
                    {
                        data["background_image"] = notification.BackgroundImage.StartsWith("http") ? notification.BackgroundImage : $"{baseUrl.TrimEnd('/')}/media/{notification.BackgroundImage}";
                    }
                    
                    var firstImage = _context.NotificationsNotificationimages.FirstOrDefault(i => i.NotificationId == notification.Id);
                    if (firstImage != null)
                    {
                        data["image_url"] = firstImage.Image.StartsWith("http") ? firstImage.Image : $"{baseUrl.TrimEnd('/')}/media/{firstImage.Image}";
                    }
                    try
                    {
                        _logger.LogInformation("[FCM DEBUG] Sending multicast to {Count} tokens. Title={Title}", tokens.Count, notification.Title);
                        successCount = await _notificationService.SendMulticastPushNotificationAsync(tokens, notification.Title, notification.Body, data);
                        _logger.LogInformation("[FCM DEBUG] Multicast result: {SuccessCount}/{Total} delivered", successCount, tokens.Count);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[FCM DEBUG] Multicast send FAILED with exception");
                        successCount = 0;
                    }
                }

                if (dto.SendWhatsapp == true)
                {
                    var userMobiles = await _context.AccountsCustomusers
                        .Where(u => users.Contains(u.Id) && !string.IsNullOrEmpty(u.Mobile))
                        .Select(u => u.Mobile)
                        .ToListAsync(ct);
                        
                    // Construct rich message
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine($"📢 *{notification.Title}*");
                    if (!string.IsNullOrWhiteSpace(notification.Subtitle)) sb.AppendLine($"_{notification.Subtitle}_\n");
                    sb.AppendLine($"{notification.Body}");
                    
                    if (!string.IsNullOrWhiteSpace(notification.Description)) sb.AppendLine($"\n📝 *Details:* {notification.Description}");
                    if (notification.EventDate.HasValue) sb.AppendLine($"\n📅 *Date:* {notification.EventDate.Value.ToString("dd MMM yyyy")}");
                    if (notification.RecurringTime.HasValue) sb.AppendLine($"\n⏰ *Time:* {notification.RecurringTime.Value.ToString("hh:mm tt")}");
                    if (!string.IsNullOrWhiteSpace(notification.LinkUrl)) sb.AppendLine($"\n🔗 *{(string.IsNullOrWhiteSpace(notification.LinkButtonText) ? "Link" : notification.LinkButtonText)}:* {notification.LinkUrl}");

                    string msg = sb.ToString();

                    // Check for image
                    byte[]? imageBytes = null;
                    string? imageFileName = null;
                    if (!string.IsNullOrEmpty(notification.BackgroundImage))
                    {
                        if (notification.BackgroundImage.StartsWith("http"))
                        {
                            using var httpClient = new System.Net.Http.HttpClient();
                            imageBytes = await httpClient.GetByteArrayAsync(notification.BackgroundImage, ct);
                            imageFileName = "image.jpg";
                        }
                        else
                        {
                            var isDev = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
                            var mediaPath = isDev 
                                ? System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "..", "shreshtlibrary", "media"))
                                : System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "media");
                            var filePath = System.IO.Path.Combine(mediaPath, notification.BackgroundImage);
                            if (System.IO.File.Exists(filePath))
                            {
                                imageBytes = await System.IO.File.ReadAllBytesAsync(filePath, ct);
                                imageFileName = System.IO.Path.GetFileName(filePath);
                            }
                        }
                    }

                    foreach (var mobile in userMobiles)
                    {
                        if (mobile != null)
                        {
                            if (imageBytes != null && imageFileName != null)
                            {
                                await _whatsAppService.SendImageAsync(mobile, imageBytes, imageFileName, msg);
                            }
                            else
                            {
                                await _whatsAppService.SendTextMessageAsync(mobile, msg);
                            }
                        }
                    }
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

            return ServiceResult<object>.Ok(new {
                id = notification.Id,
                title = notification.Title,
                body = notification.Body,
                type = notification.Type,
                success_count = notification.SuccessCount,
                failure_count = notification.FailureCount,
                total_recipients = notification.TotalRecipients
            });
        }

        public async Task<ServiceResult<object>> GetNotificationsListAsync(int page, int pageSize, CancellationToken ct = default)
        {
            pageSize = System.Math.Clamp(pageSize, 1, 100);
            
            var query = _context.NotificationsNotifications
                .Include(n => n.NotificationsNotificationimages)
                .Where(n => n.ScheduledAt == null || n.SentAt != null)
                .OrderByDescending(n => n.CreatedAt);
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
                { "send_sms", n.SendSms },
                { "background_image", n.BackgroundImage },
                { "layout", n.Layout },
                { "subtitle", n.Subtitle },
                { "description", n.Description },
                { "link_url", n.LinkUrl },
                { "link_button_text", n.LinkButtonText },
                { "images", n.NotificationsNotificationimages.Select(img => img.Image).ToList() }
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
                    delivered_at = sn.DeliveredAt,
                    push_delivered = sn.PushDelivered,
                    email_delivered = sn.EmailDelivered,
                    sms_delivered = sn.SmsDelivered
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

        public async Task<ServiceResult<bool>> ClearAllNotificationsAsync(CancellationToken ct = default)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync(ct);
                try
                {
                    await _context.NotificationsStudentnotifications.ExecuteDeleteAsync(ct);
                    await _context.NotificationsNotificationimages.ExecuteDeleteAsync(ct);
                    await _context.NotificationsNotifications.ExecuteDeleteAsync(ct);
                    
                    await transaction.CommitAsync(ct);
                    return ServiceResult<bool>.Ok(true);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(ct);
                    var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    return ServiceResult<bool>.Fail($"Failed to clear notifications: {message}");
                }
            });
        }
    }
}
