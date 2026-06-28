using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class LibraryHomeslider
{
    public long Id { get; set; }

    public string Title { get; set; } = null!;

    public string Subtitle { get; set; } = null!;

    public string? Image { get; set; }

    public string LinkUrl { get; set; } = null!;

    public bool IsActive { get; set; }

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }
}
