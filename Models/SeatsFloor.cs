using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class SeatsFloor
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int Order { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<SeatsSeatrow> SeatsSeatrows { get; set; } = new List<SeatsSeatrow>();
}
