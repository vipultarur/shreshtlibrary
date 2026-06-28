using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class StudentsReferralhistory
{
    public long Id { get; set; }

    public DateTime AppliedAt { get; set; }

    public long ReferredStudentId { get; set; }

    public long ReferrerId { get; set; }

    public virtual AccountsCustomuser ReferredStudent { get; set; } = null!;

    public virtual AccountsCustomuser Referrer { get; set; } = null!;
}
