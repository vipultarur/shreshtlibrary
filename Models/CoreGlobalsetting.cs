using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class CoreGlobalsetting
{
    public long Id { get; set; }

    public string Key { get; set; } = null!;

    public string Value { get; set; } = null!;

    public string? Description { get; set; }
}
