using System;
using System.Collections.Generic;
using System.Net;

namespace WebApplication1.Models;

public partial class CoreActivitylog
{
    public long Id { get; set; }

    public string Action { get; set; } = null!;

    public DateTime Timestamp { get; set; }

    public IPAddress? IpAddress { get; set; }

    public string Details { get; set; } = null!;

    public long? UserId { get; set; }

    public long? AdminId { get; set; }

    public virtual AccountsAdminuser? Admin { get; set; }

    public virtual AccountsCustomuser? User { get; set; }
}
