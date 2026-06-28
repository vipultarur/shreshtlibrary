using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class LibraryAchiever
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Photo { get; set; }

    public string Achievement { get; set; } = null!;

    public int Year { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? Goal { get; set; }

    public bool IsActive { get; set; }

    public bool IsFeatured { get; set; }

    public int Order { get; set; }
}
