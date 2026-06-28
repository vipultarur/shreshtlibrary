using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class NotificationsAdmininboxnotification
{
    public long Id { get; set; }

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string Type { get; set; } = null!;

    public bool IsRead { get; set; }

    public string? RelatedId { get; set; }

    public DateTime CreatedAt { get; set; }

    public long? StudentId { get; set; }

    public virtual AccountsCustomuser? Student { get; set; }
}
