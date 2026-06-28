using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class PaymentsPayment
{
    public long Id { get; set; }

    public decimal Amount { get; set; }

    public string Status { get; set; } = null!;

    public string PaymentMode { get; set; } = null!;

    public DateOnly PaymentDate { get; set; }

    public string? TransactionId { get; set; }

    public string? ReceiptUrl { get; set; }

    public string? Notes { get; set; }

    public long? MembershipId { get; set; }

    public long StudentId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string Method { get; set; } = null!;

    public DateTime? PaidAt { get; set; }

    public string? PaymentId { get; set; }

    public long? RecordedById { get; set; }

    public decimal? RefundAmount { get; set; }

    public string? RefundReason { get; set; }

    public DateTime? RefundedAt { get; set; }

    public string? TransactionRef { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public long? VerifiedById { get; set; }

    public virtual MembershipsMembership? Membership { get; set; }

    public virtual AccountsAdminuser? RecordedBy { get; set; }

    public virtual AccountsCustomuser Student { get; set; } = null!;

    public virtual AccountsAdminuser? VerifiedBy { get; set; }
}
