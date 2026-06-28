using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class MembershipsMembershipplan
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public int DurationMonths { get; set; }

    public decimal Price { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public string Benefits { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public int DurationDays { get; set; }

    public int SortOrder { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<MembershipsMembership> MembershipsMemberships { get; set; } = new List<MembershipsMembership>();
}
