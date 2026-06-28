using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class AttendanceAttendance
{
    public long Id { get; set; }

    public DateOnly Date { get; set; }

    public TimeOnly TimeIn { get; set; }

    public bool IsManual { get; set; }

    public long StudentId { get; set; }

    public long? QrCodeId { get; set; }

    public bool IsPresent { get; set; }

    public DateTime? MarkedAt { get; set; }

    public long? MarkedById { get; set; }

    public string Method { get; set; } = null!;

    public string? Note { get; set; }

    public bool LateMark { get; set; }

    public TimeOnly? TimeOut { get; set; }

    public string? TotalHours { get; set; }

    public bool UnderTime { get; set; }

    public virtual AccountsAdminuser? MarkedBy { get; set; }

    public virtual AttendanceQrcode? QrCode { get; set; }

    public virtual AccountsCustomuser Student { get; set; } = null!;
}
