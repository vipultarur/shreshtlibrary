using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class LibraryReview
{
    public long Id { get; set; }

    public int Rating { get; set; }

    public string Comment { get; set; } = null!;

    public bool IsApproved { get; set; }

    public DateTime? CreatedAt { get; set; }

    public long StudentId { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public long? ApprovedById { get; set; }

    public string? RejectionReason { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual AccountsAdminuser? ApprovedBy { get; set; }

    public virtual AccountsCustomuser Student { get; set; } = null!;
}
