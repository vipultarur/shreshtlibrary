using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class NotificationsStudentnotification
{
    public long Id { get; set; }

    public bool IsRead { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? ReadAt { get; set; }

    public long NotificationId { get; set; }

    public long StudentId { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public bool EmailDelivered { get; set; }

    public bool PushDelivered { get; set; }

    public bool SmsDelivered { get; set; }

    public virtual NotificationsNotification Notification { get; set; } = null!;

    public virtual AccountsCustomuser Student { get; set; } = null!;
}
