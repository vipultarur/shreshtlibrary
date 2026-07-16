using System;

namespace WebApplication1.Models;

public partial class LibrarySubscription
{
    public long Id { get; set; }
    public long PlanId { get; set; }
    public PlatformSubscriptionPlan Plan { get; set; } = null!;
    
    public DateTime StartDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public string Status { get; set; } = "Pending"; 
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
