using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class NotificationsDevicetoken
{
    public long Id { get; set; }

    public string Token { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public long StudentId { get; set; }

    public virtual AccountsCustomuser Student { get; set; } = null!;
}
