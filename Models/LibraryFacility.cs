using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class LibraryFacility
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public string IconKey { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public int Order { get; set; }

    public string? Image { get; set; }
}
