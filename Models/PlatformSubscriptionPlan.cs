using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class PlatformSubscriptionPlan
{
    public long Id { get; set; }
    public string PlanName { get; set; } = null!;
    public decimal MonthlyPrice { get; set; }
    public decimal QuarterlyPrice { get; set; }
    public decimal HalfYearlyPrice { get; set; }
    public decimal YearlyPrice { get; set; }
    public int MaxStudents { get; set; }
    public int MaxStaff { get; set; }
    public string Features { get; set; } = "[]"; 
    public bool IsActive { get; set; } = true;
    public bool IsRecommended { get; set; } = false;
    public int DisplayOrder { get; set; } = 0;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public ICollection<LibrarySubscription> LibrarySubscriptions { get; set; } = new List<LibrarySubscription>();
    public ICollection<LibraryPayment> LibraryPayments { get; set; } = new List<LibraryPayment>();
}
