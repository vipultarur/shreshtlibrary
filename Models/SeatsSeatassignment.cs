using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class SeatsSeatassignment
{
    public long Id { get; set; }

    public DateOnly AssignedDate { get; set; }

    public DateOnly? ReleasedDate { get; set; }

    public long SeatId { get; set; }

    public long StudentId { get; set; }

    public virtual SeatsSeat Seat { get; set; } = null!;

    public virtual AccountsCustomuser Student { get; set; } = null!;
}
