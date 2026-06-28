using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class LibraryLibraryinfo
{
    public long Id { get; set; }

    public string Rules { get; set; } = null!;

    public string Facilities { get; set; } = null!;

    public string About { get; set; } = null!;

    public string? Address { get; set; }

    public TimeOnly? CloseTime { get; set; }

    public string? Description { get; set; }

    public string? Email { get; set; }

    public string? FacebookUrl { get; set; }

    public string? GoogleMapsUrl { get; set; }

    public string? InstagramUrl { get; set; }

    public string Name { get; set; } = null!;

    public string OffDays { get; set; } = null!;

    public TimeOnly? OpenTime { get; set; }

    public string? PhonePrimary { get; set; }

    public string? PhoneSecondary { get; set; }

    public string? Tagline { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? Website { get; set; }

    public string? FeatureImage { get; set; }

    public string? LogoRectangle { get; set; }

    public string? LogoSquare { get; set; }
}
