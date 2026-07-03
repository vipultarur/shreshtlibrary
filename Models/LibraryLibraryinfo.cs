using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class LibraryLibraryinfo
{
    public long Id { get; set; }

    public string LibraryName { get; set; } = null!;
    public string Logo { get; set; } = null!;
    public string? BannerImage { get; set; }
    public string Description { get; set; } = null!;
    public int? EstablishedYear { get; set; }
    public string OwnerName { get; set; } = null!;
    public string ContactNumber { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Website { get; set; }
    public TimeOnly OpeningTime { get; set; }
    public TimeOnly ClosingTime { get; set; }
    public string? WeeklyOff { get; set; }
    public int TotalCapacity { get; set; }
    public int AvailableSeats { get; set; }
    public string AddressLine1 { get; set; } = null!;
    public string? AddressLine2 { get; set; }
    public string Area { get; set; } = null!;
    public string City { get; set; } = null!;
    public string State { get; set; } = null!;
    public string Country { get; set; } = null!;
    public string PinCode { get; set; } = null!;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string? GoogleMapUrl { get; set; }
    
    // Facilities
    public bool? Wifi { get; set; }
    public bool? Ac { get; set; }
    public bool? Cctv { get; set; }
    public bool? DrinkingWater { get; set; }
    public bool? Lockers { get; set; }
    public bool? ChargingPoints { get; set; }
    public bool? Parking { get; set; }
    public bool? ReadingArea { get; set; }
    public bool? ComputerAccess { get; set; }
    public bool? Printing { get; set; }
    
    // Socials
    public string? FacebookUrl { get; set; }
    public string? InstagramUrl { get; set; }
    public string? WhatsappNumber { get; set; }
    public string? TelegramUrl { get; set; }
    public string? YoutubeUrl { get; set; }
    public string? TwitterUrl { get; set; }
    public string? LinkedinUrl { get; set; }
    // About Content
    public string? Tagline { get; set; }
    public string? Mission { get; set; }
    public string? Vision { get; set; }
    public string? History { get; set; }
    public string? WelcomeMessage { get; set; }
    public string? Services { get; set; }
    public string? CoursesSupported { get; set; }
    public string? StatisticsDescription { get; set; }
    public string? Faq { get; set; }
    public string? Testimonials { get; set; }
    public string? EmergencyContact { get; set; }
    public string? FooterText { get; set; }

    // Membership Info
    public string? MembershipDetails { get; set; }
    public string? RegistrationProcess { get; set; }
    public string? RequiredDocuments { get; set; }
    public string? MembershipBenefits { get; set; }
    public string? LibraryRules { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
