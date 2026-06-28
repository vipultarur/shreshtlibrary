using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class MembershipsMembership
{
    public long Id { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public string Status { get; set; } = null!;

    public long StudentId { get; set; }

    public long PlanId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public long? CreatedById { get; set; }

    public bool IsActive { get; set; }

    public string? Notes { get; set; }

    public string PlanNameSnapshot { get; set; } = null!;

    public decimal PriceSnapshot { get; set; }

    public int RenewalCount { get; set; }

    public virtual AccountsAdminuser? CreatedBy { get; set; }

    public virtual ICollection<PaymentsPayment> PaymentsPayments { get; set; } = new List<PaymentsPayment>();

    public virtual MembershipsMembershipplan Plan { get; set; } = null!;

    public virtual AccountsCustomuser Student { get; set; } = null!;
}
