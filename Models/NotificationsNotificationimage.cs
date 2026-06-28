using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class NotificationsNotificationimage
{
    public long Id { get; set; }

    public string Image { get; set; } = null!;

    public int SortOrder { get; set; }

    public long NotificationId { get; set; }

    public virtual NotificationsNotification Notification { get; set; } = null!;
}
