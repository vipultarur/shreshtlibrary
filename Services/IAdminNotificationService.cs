using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Services
{
    public class NotificationPayloadDto
    {
        [FromForm(Name = "title")]
        public string Title { get; set; } = null!;

        [FromForm(Name = "body")]
        public string Body { get; set; } = null!;

        [FromForm(Name = "type")]
        public string Type { get; set; } = null!;

        [FromForm(Name = "target")]
        public string? Target { get; set; }

        [FromForm(Name = "target_group")]
        public string TargetGroup { get; set; } = null!;

        [FromForm(Name = "goal_filter")]
        public string? GoalFilter { get; set; }

        [FromForm(Name = "status_filter")]
        public string? StatusFilter { get; set; }

        [FromForm(Name = "send_push")]
        public bool? SendPush { get; set; }

        [FromForm(Name = "send_email")]
        public bool? SendEmail { get; set; }

        [FromForm(Name = "send_sms")]
        public bool? SendSms { get; set; }

        [FromForm(Name = "send_whatsapp")]
        public bool? SendWhatsapp { get; set; }

        [FromForm(Name = "scheduled_at")]
        public DateTime? ScheduledAt { get; set; }

        [FromForm(Name = "subtitle")]
        public string? Subtitle { get; set; }

        [FromForm(Name = "description")]
        public string? Description { get; set; }

        [FromForm(Name = "link_url")]
        public string? LinkUrl { get; set; }

        [FromForm(Name = "link_button_text")]
        public string? LinkButtonText { get; set; }

        [FromForm(Name = "event_date")]
        public DateTime? EventDate { get; set; }

        [FromForm(Name = "layout")]
        public string Layout { get; set; } = null!;

        [FromForm(Name = "audience")]
        public string Audience { get; set; } = null!;

        [FromForm(Name = "display_mode")]
        public string DisplayMode { get; set; } = null!;

        [FromForm(Name = "recurring_time")]
        public TimeOnly? RecurringTime { get; set; }

        [FromForm(Name = "expires_at")]
        public DateTime? ExpiresAt { get; set; }

        [FromForm(Name = "selected_students")]
        public string? SelectedStudents { get; set; }

        [FromForm(Name = "background_image")]
        public IFormFile? BackgroundImage { get; set; }

        [FromForm(Name = "images")]
        public List<IFormFile>? Images { get; set; }
    }

    public interface IAdminNotificationService
    {
        Task<ServiceResult<object>> GetNotificationTemplatesAsync(CancellationToken ct = default);
        Task<ServiceResult<object>> GetScheduledNotificationsAsync(CancellationToken ct = default);
        Task<ServiceResult<object>> CancelScheduledNotificationAsync(long pk, CancellationToken ct = default);
        Task<ServiceResult<object>> ProcessNotificationAsync(NotificationPayloadDto dto, bool isSchedule, CancellationToken ct = default);
        Task<ServiceResult<object>> GetNotificationsListAsync(int page, int pageSize, CancellationToken ct = default);
        Task<ServiceResult<object>> GetNotificationDetailAsync(long pk, CancellationToken ct = default);
        Task<ServiceResult<object>> GetNotificationRecipientsAsync(long pk, CancellationToken ct = default);
        Task<ServiceResult<object>> GetInboxNotificationsAsync(CancellationToken ct = default);
        Task<ServiceResult<bool>> MarkInboxActionAsync(long pk, string action, CancellationToken ct = default);
        Task<ServiceResult<bool>> DeleteInboxNotificationAsync(long pk, CancellationToken ct = default);
        Task<ServiceResult<bool>> ClearAllNotificationsAsync(CancellationToken ct = default);
    }
}
