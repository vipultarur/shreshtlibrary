using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class AccountsAuthtokenrevocation
{
    public long Id { get; set; }

    public string TokenHash { get; set; } = null!;

    public string Jti { get; set; } = null!;

    public string UserIdentifier { get; set; } = null!;

    public DateTime RevokedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }
}
