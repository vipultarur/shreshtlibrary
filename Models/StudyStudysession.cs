using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class StudyStudysession
{
    public long Id { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public int DurationMinutes { get; set; }

    public long StudentId { get; set; }

    public int PausedMinutes { get; set; }

    public string Status { get; set; } = null!;

    public virtual AccountsCustomuser Student { get; set; } = null!;
}
