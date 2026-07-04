using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Controllers;
using WebApplication1.Models.DTOs.Notifications;

namespace WebApplication1.Services
{
    public interface IStudentNotificationService
    {
        Task<ServiceResult<object>> GetNotificationsAsync(long userId, int page = 1, int page_size = 20, CancellationToken ct = default);
        Task<ServiceResult<object>> MarkNotificationReadAsync(long userId, int id, CancellationToken ct = default);
        Task<ServiceResult<object>> RegisterDeviceAsync(long userId, DeviceTokenPayload payload, CancellationToken ct = default);
    }

    public class StudentNotificationService : IStudentNotificationService
    {
        private readonly ApplicationDbContext _context;

        public StudentNotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceResult<object>> GetNotificationsAsync(long userId, int page = 1, int page_size = 20, CancellationToken ct = default)
        {
            page_size = System.Math.Clamp(page_size, 1, 100);

            var query = _context.NotificationsStudentnotifications
                .AsNoTracking()
                .Include(sn => sn.Notification)
                    .ThenInclude(n => n.NotificationsNotificationimages)
                .Where(sn => sn.StudentId == userId && sn.Notification.SentAt != null)
                .OrderByDescending(sn => sn.Notification.CreatedAt);

            var totalCount = await query.CountAsync(ct);
            var totalPages = (int)Math.Ceiling((double)totalCount / page_size);

            var dbRows = await query.Skip((page - 1) * page_size).Take(page_size).ToListAsync(ct);

            var data = dbRows.Select(sn => new Dictionary<string, object?> {
                    { "id", sn.NotificationId },
                    { "title", sn.Notification.Title },
                    { "body", sn.Notification.Body },
                    { "type", sn.Notification.Type },
                    { "is_read", sn.IsRead },
                    { "read_at", sn.ReadAt },
                    { "created_at", sn.Notification.CreatedAt },
                    { "sent_at", sn.Notification.SentAt?.ToString("MMM dd, yyyy h:mm tt") },
                    { "subtitle", string.IsNullOrEmpty(sn.Notification.Subtitle) ? null : sn.Notification.Subtitle },
                    { "description", string.IsNullOrEmpty(sn.Notification.Description) ? null : sn.Notification.Description },
                    { "link_url", string.IsNullOrEmpty(sn.Notification.LinkUrl) ? null : sn.Notification.LinkUrl },
                    { "link_button_text", string.IsNullOrEmpty(sn.Notification.LinkButtonText) ? null : sn.Notification.LinkButtonText },
                    { "layout", sn.Notification.Layout },
                    { "display_mode", sn.Notification.DisplayMode },
                    { "background_image", string.IsNullOrEmpty(sn.Notification.BackgroundImage) ? null : $"/media/{sn.Notification.BackgroundImage}" },
                    { "event_date", sn.Notification.EventDate?.ToString("yyyy-MM-dd") },
                    { "images", sn.Notification.NotificationsNotificationimages
                        .Select(img => $"/media/{img.Image}")
                        .Where(path => !string.IsNullOrEmpty(path))
                        .ToList() }
                }).ToList();

            return ServiceResult<object>.Ok(new { data = data, count = totalCount, total_pages = totalPages == 0 ? 1 : totalPages, current_page = page });
        }

        public async Task<ServiceResult<object>> MarkNotificationReadAsync(long userId, int id, CancellationToken ct = default)
        {
            var sn = await _context.NotificationsStudentnotifications
                .FirstOrDefaultAsync(s => s.StudentId == userId && s.NotificationId == id, ct);
                
            if (sn == null)
            {
                return ServiceResult<object>.NotFound("Notification not found.");
            }

            if (!sn.IsRead)
            {
                sn.IsRead = true;
                sn.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(ct);
            }

            return ServiceResult<object>.Ok(null, "Notification marked as read");
        }

        public async Task<ServiceResult<object>> RegisterDeviceAsync(long userId, DeviceTokenPayload payload, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(payload?.Token))
            {
                return ServiceResult<object>.Fail("Token is required.");
            }

            var existingToken = await _context.NotificationsDevicetokens
                .FirstOrDefaultAsync(dt => dt.Token == payload.Token, ct);

            if (existingToken != null)
            {
                if (existingToken.StudentId != userId)
                {
                    existingToken.StudentId = userId;
                }
            }
            else
            {
                _context.NotificationsDevicetokens.Add(new NotificationsDevicetoken
                {
                    StudentId = userId,
                    Token = payload.Token,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync(ct);
            return ServiceResult<object>.Ok(null, "Token registered successfully");
        }
    }
}
