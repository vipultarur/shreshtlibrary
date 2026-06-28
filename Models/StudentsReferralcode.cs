using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class StudentsReferralcode
{
    public long Id { get; set; }

    public string Code { get; set; } = null!;

    public int UsedByCount { get; set; }

    public string? BenefitGiven { get; set; }

    public long StudentId { get; set; }

    public virtual AccountsCustomuser Student { get; set; } = null!;
}
