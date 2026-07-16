using System;

namespace WebApplication1.Models;

public partial class LibraryPayment
{
    public long Id { get; set; }
    public long PlanId { get; set; }
    public PlatformSubscriptionPlan Plan { get; set; } = null!;
    
    public decimal Amount { get; set; }
    public int DurationDays { get; set; }
    public string UtrNumber { get; set; } = null!;
    public string? ScreenshotPath { get; set; }
    public string Status { get; set; } = "Pending";
    
    public long? ApprovedById { get; set; }
    public AccountsAdminuser? ApprovedBy { get; set; }
    
    public DateTime SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
}
