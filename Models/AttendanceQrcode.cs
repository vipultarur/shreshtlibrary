using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class AttendanceQrcode
{
    public long Id { get; set; }

    public string Code { get; set; } = null!;

    public DateOnly ValidDate { get; set; }

    public bool IsExpired { get; set; }

    public DateTime ExpiryTimestamp { get; set; }

    public long? CreatedById { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public string GenerationMethod { get; set; } = null!;

    public bool IsActive { get; set; }

    public string QrHash { get; set; } = null!;

    public Guid? Token { get; set; }

    public virtual ICollection<AttendanceAttendance> AttendanceAttendances { get; set; } = new List<AttendanceAttendance>();

    public virtual AccountsAdminuser? CreatedBy { get; set; }
}
