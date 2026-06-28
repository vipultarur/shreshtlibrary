using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class AccountsAdminuser
{
    public long Id { get; set; }

    public string Username { get; set; } = null!;

    public string? Email { get; set; }

    public string Password { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string? Mobile { get; set; }

    public string Role { get; set; } = null!;

    public string Permissions { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime DateJoined { get; set; }

    public DateTime? LastLogin { get; set; }

    public Guid? SupabaseUid { get; set; }

    public long? CreatedById { get; set; }

    public string? ProfileImage { get; set; }

    public virtual ICollection<AttendanceAttendance> AttendanceAttendances { get; set; } = new List<AttendanceAttendance>();

    public virtual ICollection<AttendanceHoliday> AttendanceHolidays { get; set; } = new List<AttendanceHoliday>();

    public virtual ICollection<AttendanceQrcode> AttendanceQrcodes { get; set; } = new List<AttendanceQrcode>();

    public virtual ICollection<CoreActivitylog> CoreActivitylogs { get; set; } = new List<CoreActivitylog>();

    public virtual AccountsAdminuser? CreatedBy { get; set; }

    public virtual ICollection<AccountsAdminuser> InverseCreatedBy { get; set; } = new List<AccountsAdminuser>();

    public virtual ICollection<LibraryReview> LibraryReviews { get; set; } = new List<LibraryReview>();

    public virtual ICollection<MembershipsMembership> MembershipsMemberships { get; set; } = new List<MembershipsMembership>();

    public virtual ICollection<NotificationsNotification> NotificationsNotifications { get; set; } = new List<NotificationsNotification>();

    public virtual ICollection<PaymentsPayment> PaymentsPaymentRecordedBies { get; set; } = new List<PaymentsPayment>();

    public virtual ICollection<PaymentsPayment> PaymentsPaymentVerifiedBies { get; set; } = new List<PaymentsPayment>();

    public virtual ICollection<SeatsSeatchangelog> SeatsSeatchangelogs { get; set; } = new List<SeatsSeatchangelog>();

    public virtual ICollection<SeatsSeat> SeatsSeats { get; set; } = new List<SeatsSeat>();

    public virtual ICollection<StudentsStudentprofile> StudentsStudentprofiles { get; set; } = new List<StudentsStudentprofile>();
}
