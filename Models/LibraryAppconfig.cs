using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class LibraryAppconfig
{
    public long Id { get; set; }

    public bool IsPremiumGatingEnabled { get; set; }

    public string ExpiryDialogTitle { get; set; } = null!;

    public string ExpiryDialogMessage { get; set; } = null!;

    public bool AllowNonPremiumNotifications { get; set; }

    public bool AllowNonPremiumLibraryInfo { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool AllowNonPremiumSliders { get; set; }

    public int DefaultAllowedStudyMinutes { get; set; }

    public string ExpiredStudentPermissions { get; set; } = null!;

    public bool EnableWhatsappService { get; set; }
}
