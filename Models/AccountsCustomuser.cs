using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class AccountsCustomuser
{
    public long Id { get; set; }

    public string Password { get; set; } = null!;

    public DateTime? LastLogin { get; set; }

    public bool IsSuperuser { get; set; }

    public string Username { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public bool IsStaff { get; set; }

    public DateTime DateJoined { get; set; }

    public string? Email { get; set; }

    public string? Mobile { get; set; }

    public string Role { get; set; } = null!;

    public string? Otp { get; set; }

    public DateTime? OtpExpiry { get; set; }

    public bool IsActive { get; set; }

    public Guid? SupabaseUid { get; set; }

    public int OtpAttempts { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ICollection<AccountsCustomuserGroup> AccountsCustomuserGroups { get; set; } = new List<AccountsCustomuserGroup>();

    public virtual ICollection<AccountsCustomuserUserPermission> AccountsCustomuserUserPermissions { get; set; } = new List<AccountsCustomuserUserPermission>();

    public virtual ICollection<AttendanceAttendance> AttendanceAttendances { get; set; } = new List<AttendanceAttendance>();

    public virtual ICollection<CoreActivitylog> CoreActivitylogs { get; set; } = new List<CoreActivitylog>();

    public virtual ICollection<DjangoAdminLog> DjangoAdminLogs { get; set; } = new List<DjangoAdminLog>();

    public virtual ICollection<LibraryReview> LibraryReviews { get; set; } = new List<LibraryReview>();

    public virtual ICollection<MembershipsMembership> MembershipsMemberships { get; set; } = new List<MembershipsMembership>();

    public virtual ICollection<NotificationsAdmininboxnotification> NotificationsAdmininboxnotifications { get; set; } = new List<NotificationsAdmininboxnotification>();

    public virtual ICollection<NotificationsDevicetoken> NotificationsDevicetokens { get; set; } = new List<NotificationsDevicetoken>();

    public virtual ICollection<NotificationsStudentnotification> NotificationsStudentnotifications { get; set; } = new List<NotificationsStudentnotification>();

    public virtual ICollection<PaymentsPayment> PaymentsPayments { get; set; } = new List<PaymentsPayment>();

    public virtual SeatsSeat? SeatsSeat { get; set; }

    public virtual ICollection<SeatsSeatassignment> SeatsSeatassignments { get; set; } = new List<SeatsSeatassignment>();

    public virtual ICollection<SeatsSeatchangelog> SeatsSeatchangelogs { get; set; } = new List<SeatsSeatchangelog>();

    public virtual ICollection<StudentsReferralcode> StudentsReferralcodes { get; set; } = new List<StudentsReferralcode>();

    public virtual ICollection<StudentsReferralhistory> StudentsReferralhistoryReferredStudents { get; set; } = new List<StudentsReferralhistory>();

    public virtual ICollection<StudentsReferralhistory> StudentsReferralhistoryReferrers { get; set; } = new List<StudentsReferralhistory>();

    public virtual StudentsStudentprofile? StudentsStudentprofile { get; set; }

    public virtual ICollection<StudyStudysession> StudyStudysessions { get; set; } = new List<StudyStudysession>();

    public virtual ICollection<TokenBlacklistOutstandingtoken> TokenBlacklistOutstandingtokens { get; set; } = new List<TokenBlacklistOutstandingtoken>();
}
