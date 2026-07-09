using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class StudentsStudentprofile
{
    public long Id { get; set; }

    public string Goal { get; set; } = null!;

    public DateOnly? Dob { get; set; }

    public string? Caste { get; set; }

    public string? Address { get; set; }

    public string? ProfilePhoto { get; set; }

    public string? ParentMobile { get; set; }

    public long UserId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string Gender { get; set; } = null!;

    public string? MiddleName { get; set; }

    public string PreferredLanguage { get; set; } = null!;

    public long? ReferredById { get; set; }

    public string Status { get; set; } = null!;

    public string? StudentId { get; set; }

    public DateTime? SuspendedAt { get; set; }

    public long? SuspendedById { get; set; }

    public string? SuspensionReason { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? AllowedStudyMinutes { get; set; }

    public DateOnly JoiningDate { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.Column("is_deleted")]
    public bool IsDeleted { get; set; }

    public virtual ICollection<StudentsStudentprofile> InverseReferredBy { get; set; } = new List<StudentsStudentprofile>();

    public virtual StudentsStudentprofile? ReferredBy { get; set; }

    public virtual AccountsAdminuser? SuspendedBy { get; set; }

    public virtual AccountsCustomuser User { get; set; } = null!;
}
