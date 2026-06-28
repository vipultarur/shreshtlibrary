using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class NotificationsNotification
{
    public long Id { get; set; }

    public string Title { get; set; } = null!;

    public string Body { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string TargetGroup { get; set; } = null!;

    public DateTime? SentAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public long? CreatedById { get; set; }

    public int FailureCount { get; set; }

    public string? GoalFilter { get; set; }

    public DateTime? ScheduledAt { get; set; }

    public bool SendEmail { get; set; }

    public bool SendPush { get; set; }

    public bool SendSms { get; set; }

    public string? StatusFilter { get; set; }

    public int SuccessCount { get; set; }

    public string Target { get; set; } = null!;

    public int TotalRecipients { get; set; }

    public string Audience { get; set; } = null!;

    public string? BackgroundImage { get; set; }

    public string Description { get; set; } = null!;

    public string DisplayMode { get; set; } = null!;

    public DateOnly? EventDate { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public string Layout { get; set; } = null!;

    public string LinkButtonText { get; set; } = null!;

    public string LinkUrl { get; set; } = null!;

    public TimeOnly? RecurringTime { get; set; }

    public string Subtitle { get; set; } = null!;

    public virtual AccountsAdminuser? CreatedBy { get; set; }

    public virtual ICollection<NotificationsNotificationimage> NotificationsNotificationimages { get; set; } = new List<NotificationsNotificationimage>();

    public virtual ICollection<NotificationsStudentnotification> NotificationsStudentnotifications { get; set; } = new List<NotificationsStudentnotification>();
}
