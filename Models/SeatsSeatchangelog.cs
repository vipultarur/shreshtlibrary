using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class SeatsSeatchangelog
{
    public long Id { get; set; }

    public string Action { get; set; } = null!;

    public string? Reason { get; set; }

    public DateTime ChangedAt { get; set; }

    public long? ChangedById { get; set; }

    public long? PreviousSeatId { get; set; }

    public long SeatId { get; set; }

    public long? StudentId { get; set; }

    public virtual AccountsAdminuser? ChangedBy { get; set; }

    public virtual SeatsSeat? PreviousSeat { get; set; }

    public virtual SeatsSeat Seat { get; set; } = null!;

    public virtual AccountsCustomuser? Student { get; set; }
}
