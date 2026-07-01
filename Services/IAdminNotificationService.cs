using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace WebApplication1.Services
{
    public class NotificationPayloadDto
    {
        public string Title { get; set; } = null!;
        public string Body { get; set; } = null!;
        public string Type { get; set; } = null!;
        public string? Target { get; set; }
        public string TargetGroup { get; set; } = null!;
        public string? GoalFilter { get; set; }
        public string? StatusFilter { get; set; }
        public bool? SendPush { get; set; }
        public bool? SendEmail { get; set; }
        public bool? SendSms { get; set; }
        public DateTime? ScheduledAt { get; set; }
        public string? Subtitle { get; set; }
        public string? Description { get; set; }
        public string? LinkUrl { get; set; }
        public string? LinkButtonText { get; set; }
        public DateTime? EventDate { get; set; }
        public string Layout { get; set; } = null!;
        public string Audience { get; set; } = null!;
        public string DisplayMode { get; set; } = null!;
        public TimeOnly? RecurringTime { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string? SelectedStudents { get; set; }
        public IFormFile? BackgroundImage { get; set; }
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
}
