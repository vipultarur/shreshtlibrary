using System;

namespace WebApplication1.Models;

public partial class LibraryGalleryImage
{
    public long Id { get; set; }
    public string ImageUrl { get; set; } = null!;
    public string? Caption { get; set; }
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; }
}
