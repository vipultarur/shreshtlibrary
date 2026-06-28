using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class AttendanceHoliday
{
    public long Id { get; set; }

    public DateOnly Date { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public long? CreatedById { get; set; }

    public virtual AccountsAdminuser? CreatedBy { get; set; }
}
