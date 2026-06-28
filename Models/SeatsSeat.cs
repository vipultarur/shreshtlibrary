using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class SeatsSeat
{
    public long Id { get; set; }

    public string Floor { get; set; } = null!;

    public string Row { get; set; } = null!;

    public string SeatNumber { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime? AssignedAt { get; set; }

    public long? AssignedById { get; set; }

    public string? Notes { get; set; }

    public long? StudentId { get; set; }

    public long? RowRefId { get; set; }

    public bool IsReservedForGirls { get; set; }

    public virtual AccountsAdminuser? AssignedBy { get; set; }

    public virtual SeatsSeatrow? RowRef { get; set; }

    public virtual ICollection<SeatsSeatassignment> SeatsSeatassignments { get; set; } = new List<SeatsSeatassignment>();

    public virtual ICollection<SeatsSeatchangelog> SeatsSeatchangelogPreviousSeats { get; set; } = new List<SeatsSeatchangelog>();

    public virtual ICollection<SeatsSeatchangelog> SeatsSeatchangelogSeats { get; set; } = new List<SeatsSeatchangelog>();

    public virtual AccountsCustomuser? Student { get; set; }
}
