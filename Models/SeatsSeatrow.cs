using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class SeatsSeatrow
{
    public long Id { get; set; }

    public string Label { get; set; } = null!;

    public int Order { get; set; }

    public long FloorId { get; set; }

    public virtual SeatsFloor Floor { get; set; } = null!;

    public virtual ICollection<SeatsSeat> SeatsSeats { get; set; } = new List<SeatsSeat>();
}
