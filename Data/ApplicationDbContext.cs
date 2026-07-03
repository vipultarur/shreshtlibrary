using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AccountsAdminuser> AccountsAdminusers { get; set; }

    public virtual DbSet<AccountsAuthtokenrevocation> AccountsAuthtokenrevocations { get; set; }

    public virtual DbSet<AccountsCustomuser> AccountsCustomusers { get; set; }

    public virtual DbSet<AccountsCustomuserGroup> AccountsCustomuserGroups { get; set; }

    public virtual DbSet<AccountsCustomuserUserPermission> AccountsCustomuserUserPermissions { get; set; }

    public virtual DbSet<AttendanceAttendance> AttendanceAttendances { get; set; }

    public virtual DbSet<AttendanceHoliday> AttendanceHolidays { get; set; }

    public virtual DbSet<AttendanceQrcode> AttendanceQrcodes { get; set; }

    public virtual DbSet<AuthGroup> AuthGroups { get; set; }

    public virtual DbSet<AuthGroupPermission> AuthGroupPermissions { get; set; }

    public virtual DbSet<AuthPermission> AuthPermissions { get; set; }

    public virtual DbSet<CoreActivitylog> CoreActivitylogs { get; set; }

    public virtual DbSet<CoreGlobalsetting> CoreGlobalsettings { get; set; }

    public virtual DbSet<DjangoAdminLog> DjangoAdminLogs { get; set; }

    public virtual DbSet<DjangoContentType> DjangoContentTypes { get; set; }

    public virtual DbSet<DjangoMigration> DjangoMigrations { get; set; }

    public virtual DbSet<DjangoSession> DjangoSessions { get; set; }

    public virtual DbSet<LibraryAchiever> LibraryAchievers { get; set; }

    public virtual DbSet<LibraryAppconfig> LibraryAppconfigs { get; set; }

    public virtual DbSet<LibraryDatabasefile> LibraryDatabasefiles { get; set; }

    public virtual DbSet<LibraryFacility> LibraryFacilities { get; set; }

    public virtual DbSet<LibraryGalleryImage> LibraryGalleryImages { get; set; }

    public virtual DbSet<LibraryHomeslider> LibraryHomesliders { get; set; }

    public virtual DbSet<LibraryLibraryinfo> LibraryLibraryinfos { get; set; }

    public virtual DbSet<LibraryReview> LibraryReviews { get; set; }

    public virtual DbSet<MembershipsMembership> MembershipsMemberships { get; set; }

    public virtual DbSet<MembershipsMembershipplan> MembershipsMembershipplans { get; set; }

    public virtual DbSet<NotificationsAdmininboxnotification> NotificationsAdmininboxnotifications { get; set; }

    public virtual DbSet<NotificationsDevicetoken> NotificationsDevicetokens { get; set; }

    public virtual DbSet<NotificationsNotification> NotificationsNotifications { get; set; }

    public virtual DbSet<NotificationsNotificationimage> NotificationsNotificationimages { get; set; }

    public virtual DbSet<NotificationsStudentnotification> NotificationsStudentnotifications { get; set; }

    public virtual DbSet<PaymentsPayment> PaymentsPayments { get; set; }

    public virtual DbSet<SeatsFloor> SeatsFloors { get; set; }

    public virtual DbSet<SeatsSeat> SeatsSeats { get; set; }

    public virtual DbSet<SeatsSeatassignment> SeatsSeatassignments { get; set; }

    public virtual DbSet<SeatsSeatchangelog> SeatsSeatchangelogs { get; set; }

    public virtual DbSet<SeatsSeatrow> SeatsSeatrows { get; set; }

    public virtual DbSet<StudentsReferralcode> StudentsReferralcodes { get; set; }

    public virtual DbSet<StudentsReferralhistory> StudentsReferralhistories { get; set; }

    public virtual DbSet<StudentsStudentprofile> StudentsStudentprofiles { get; set; }

    public virtual DbSet<StudyStudysession> StudyStudysessions { get; set; }

    public virtual DbSet<TokenBlacklistBlacklistedtoken> TokenBlacklistBlacklistedtokens { get; set; }

    public virtual DbSet<TokenBlacklistOutstandingtoken> TokenBlacklistOutstandingtokens { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // Typically configuration is handled in Program.cs.
            // Leave this empty or use a fallback if absolutely necessary.
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresEnum("auth", "aal_level", new[] { "aal1", "aal2", "aal3" })
            .HasPostgresEnum("auth", "code_challenge_method", new[] { "s256", "plain" })
            .HasPostgresEnum("auth", "factor_status", new[] { "unverified", "verified" })
            .HasPostgresEnum("auth", "factor_type", new[] { "totp", "webauthn", "phone" })
            .HasPostgresEnum("auth", "oauth_authorization_status", new[] { "pending", "approved", "denied", "expired" })
            .HasPostgresEnum("auth", "oauth_client_type", new[] { "public", "confidential" })
            .HasPostgresEnum("auth", "oauth_registration_type", new[] { "dynamic", "manual" })
            .HasPostgresEnum("auth", "oauth_response_type", new[] { "code" })
            .HasPostgresEnum("auth", "one_time_token_type", new[] { "confirmation_token", "reauthentication_token", "recovery_token", "email_change_token_new", "email_change_token_current", "phone_change_token" })
            .HasPostgresEnum("realtime", "action", new[] { "INSERT", "UPDATE", "DELETE", "TRUNCATE", "ERROR" })
            .HasPostgresEnum("realtime", "equality_op", new[] { "eq", "neq", "lt", "lte", "gt", "gte", "in" })
            .HasPostgresEnum("storage", "buckettype", new[] { "STANDARD", "ANALYTICS", "VECTOR" })
            .HasPostgresExtension("extensions", "pg_stat_statements")
            .HasPostgresExtension("extensions", "pgcrypto")
            .HasPostgresExtension("extensions", "uuid-ossp")
            .HasPostgresExtension("vault", "supabase_vault");

        modelBuilder.Entity<AccountsAdminuser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("accounts_adminuser_pkey");

            entity.ToTable("accounts_adminuser");

            entity.HasIndex(e => e.CreatedById, "accounts_adminuser_created_by_id_02f160a4");

            entity.HasIndex(e => e.Email, "accounts_adminuser_email_5110578e_like").HasOperators(new[] { "varchar_pattern_ops" });

            entity.HasIndex(e => e.Email, "accounts_adminuser_email_key").IsUnique();

            entity.HasIndex(e => e.Mobile, "accounts_adminuser_mobile_3d5b9327_like").HasOperators(new[] { "varchar_pattern_ops" });

            entity.HasIndex(e => e.Mobile, "accounts_adminuser_mobile_key").IsUnique();

            entity.HasIndex(e => e.SupabaseUid, "accounts_adminuser_supabase_uid_key").IsUnique();

            entity.HasIndex(e => e.Username, "accounts_adminuser_username_4d9b2ca6_like").HasOperators(new[] { "varchar_pattern_ops" });

            entity.HasIndex(e => e.Username, "accounts_adminuser_username_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedById).HasColumnName("created_by_id");
            entity.Property(e => e.DateJoined).HasColumnName("date_joined");
            entity.Property(e => e.Email)
                .HasMaxLength(254)
                .HasColumnName("email");
            entity.Property(e => e.FirstName)
                .HasMaxLength(150)
                .HasColumnName("first_name");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.LastLogin).HasColumnName("last_login");
            entity.Property(e => e.LastName)
                .HasMaxLength(150)
                .HasColumnName("last_name");
            entity.Property(e => e.Mobile)
                .HasMaxLength(15)
                .HasColumnName("mobile");
            entity.Property(e => e.Password)
                .HasMaxLength(128)
                .HasColumnName("password");
            entity.Property(e => e.Permissions)
                .HasColumnType("jsonb")
                .HasColumnName("permissions");
            entity.Property(e => e.ProfileImage)
                .HasMaxLength(100)
                .HasColumnName("profile_image");
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .HasColumnName("role");
            entity.Property(e => e.SupabaseUid).HasColumnName("supabase_uid");
            entity.Property(e => e.Username)
                .HasMaxLength(150)
                .HasColumnName("username");

            entity.HasOne(d => d.CreatedBy).WithMany(p => p.InverseCreatedBy)
                .HasForeignKey(d => d.CreatedById)
                .HasConstraintName("accounts_adminuser_created_by_id_02f160a4_fk_accounts_");
        });

        modelBuilder.Entity<AccountsAuthtokenrevocation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("accounts_authtokenrevocation_pkey");

            entity.ToTable("accounts_authtokenrevocation");

            entity.HasIndex(e => e.ExpiresAt, "accounts_au_expires_474335_idx");

            entity.HasIndex(e => e.Jti, "accounts_authtokenrevocation_jti_53823217");

            entity.HasIndex(e => e.Jti, "accounts_authtokenrevocation_jti_53823217_like").HasOperators(new[] { "varchar_pattern_ops" });

            entity.HasIndex(e => e.TokenHash, "accounts_authtokenrevocation_token_hash_69826360_like").HasOperators(new[] { "varchar_pattern_ops" });

            entity.HasIndex(e => e.TokenHash, "accounts_authtokenrevocation_token_hash_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.Jti)
                .HasMaxLength(255)
                .HasColumnName("jti");
            entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");
            entity.Property(e => e.TokenHash)
                .HasMaxLength(64)
                .HasColumnName("token_hash");
            entity.Property(e => e.UserIdentifier)
                .HasMaxLength(255)
                .HasColumnName("user_identifier");
        });

        modelBuilder.Entity<AccountsCustomuser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("accounts_customuser_pkey");

            entity.ToTable("accounts_customuser");

            entity.HasIndex(e => e.Email, "accounts_customuser_email_4fd8e7ce_like").HasOperators(new[] { "varchar_pattern_ops" });

            entity.HasIndex(e => e.Email, "accounts_customuser_email_key").IsUnique();

            entity.HasIndex(e => e.Mobile, "accounts_customuser_mobile_a211a2ea_like").HasOperators(new[] { "varchar_pattern_ops" });

            entity.HasIndex(e => e.Mobile, "accounts_customuser_mobile_key").IsUnique();

            entity.HasIndex(e => e.SupabaseUid, "accounts_customuser_supabase_uid_key").IsUnique();

            entity.HasIndex(e => e.Username, "accounts_customuser_username_722f3555_like").HasOperators(new[] { "varchar_pattern_ops" });

            entity.HasIndex(e => e.Username, "accounts_customuser_username_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DateJoined).HasColumnName("date_joined");
            entity.Property(e => e.Email)
                .HasMaxLength(254)
                .HasColumnName("email");
            entity.Property(e => e.FirstName)
                .HasMaxLength(150)
                .HasColumnName("first_name");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.IsStaff).HasColumnName("is_staff");
            entity.Property(e => e.IsSuperuser).HasColumnName("is_superuser");
            entity.Property(e => e.LastLogin).HasColumnName("last_login");
            entity.Property(e => e.LastName)
                .HasMaxLength(150)
                .HasColumnName("last_name");
            entity.Property(e => e.Mobile)
                .HasMaxLength(15)
                .HasColumnName("mobile");
            entity.Property(e => e.Otp)
                .HasMaxLength(128)
                .HasColumnName("otp");
            entity.Property(e => e.OtpAttempts).HasColumnName("otp_attempts");
            entity.Property(e => e.OtpExpiry).HasColumnName("otp_expiry");
            entity.Property(e => e.Password)
                .HasMaxLength(128)
                .HasColumnName("password");
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .HasColumnName("role");
            entity.Property(e => e.SupabaseUid).HasColumnName("supabase_uid");
            entity.Property(e => e.Username)
                .HasMaxLength(150)
                .HasColumnName("username");
        });

        modelBuilder.Entity<AccountsCustomuserGroup>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("accounts_customuser_groups_pkey");

            entity.ToTable("accounts_customuser_groups");

            entity.HasIndex(e => e.CustomuserId, "accounts_customuser_groups_customuser_id_bc55088e");

            entity.HasIndex(e => new { e.CustomuserId, e.GroupId }, "accounts_customuser_groups_customuser_id_group_id_c074bdcb_uniq").IsUnique();

            entity.HasIndex(e => e.GroupId, "accounts_customuser_groups_group_id_86ba5f9e");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CustomuserId).HasColumnName("customuser_id");
            entity.Property(e => e.GroupId).HasColumnName("group_id");

            entity.HasOne(d => d.Customuser).WithMany(p => p.AccountsCustomuserGroups)
                .HasForeignKey(d => d.CustomuserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("accounts_customuser__customuser_id_bc55088e_fk_accounts_");

            entity.HasOne(d => d.Group).WithMany(p => p.AccountsCustomuserGroups)
                .HasForeignKey(d => d.GroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("accounts_customuser_groups_group_id_86ba5f9e_fk_auth_group_id");
        });

        modelBuilder.Entity<AccountsCustomuserUserPermission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("accounts_customuser_user_permissions_pkey");

            entity.ToTable("accounts_customuser_user_permissions");

            entity.HasIndex(e => new { e.CustomuserId, e.PermissionId }, "accounts_customuser_user_customuser_id_permission_9632a709_uniq").IsUnique();

            entity.HasIndex(e => e.CustomuserId, "accounts_customuser_user_permissions_customuser_id_0deaefae");

            entity.HasIndex(e => e.PermissionId, "accounts_customuser_user_permissions_permission_id_aea3d0e5");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CustomuserId).HasColumnName("customuser_id");
            entity.Property(e => e.PermissionId).HasColumnName("permission_id");

            entity.HasOne(d => d.Customuser).WithMany(p => p.AccountsCustomuserUserPermissions)
                .HasForeignKey(d => d.CustomuserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("accounts_customuser__customuser_id_0deaefae_fk_accounts_");

            entity.HasOne(d => d.Permission).WithMany(p => p.AccountsCustomuserUserPermissions)
                .HasForeignKey(d => d.PermissionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("accounts_customuser__permission_id_aea3d0e5_fk_auth_perm");
        });

        modelBuilder.Entity<AttendanceAttendance>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("attendance_attendance_pkey");

            entity.ToTable("attendance_attendance");

            entity.HasIndex(e => e.Date, "attendance__date_61f2e1_idx");

            entity.HasIndex(e => e.IsPresent, "attendance__is_pres_772c00_idx");

            entity.HasIndex(e => new { e.StudentId, e.Date }, "attendance__student_76a8d7_idx");

            entity.HasIndex(e => e.MarkedById, "attendance_attendance_marked_by_id_0698c76f");

            entity.HasIndex(e => e.QrCodeId, "attendance_attendance_qr_code_id_e23eb1f8");

            entity.HasIndex(e => e.StudentId, "attendance_attendance_student_id_94863613");

            entity.HasIndex(e => new { e.StudentId, e.Date }, "attendance_attendance_student_id_date_167892e4_uniq").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.IsManual).HasColumnName("is_manual");
            entity.Property(e => e.IsPresent).HasColumnName("is_present");
            entity.Property(e => e.LateMark).HasColumnName("late_mark");
            entity.Property(e => e.MarkedAt).HasColumnName("marked_at");
            entity.Property(e => e.MarkedById).HasColumnName("marked_by_id");
            entity.Property(e => e.Method)
                .HasMaxLength(20)
                .HasColumnName("method");
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.QrCodeId).HasColumnName("qr_code_id");
            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.TimeIn).HasColumnName("time_in");
            entity.Property(e => e.TimeOut).HasColumnName("time_out");
            entity.Property(e => e.TotalHours)
                .HasMaxLength(10)
                .HasColumnName("total_hours");
            entity.Property(e => e.UnderTime).HasColumnName("under_time");

            entity.HasOne(d => d.MarkedBy).WithMany(p => p.AttendanceAttendances)
                .HasForeignKey(d => d.MarkedById)
                .HasConstraintName("attendance_attendanc_marked_by_id_0698c76f_fk_accounts_");

            entity.HasOne(d => d.QrCode).WithMany(p => p.AttendanceAttendances)
                .HasForeignKey(d => d.QrCodeId)
                .HasConstraintName("attendance_attendanc_qr_code_id_e23eb1f8_fk_attendanc");

            entity.HasOne(d => d.Student).WithMany(p => p.AttendanceAttendances)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("attendance_attendanc_student_id_94863613_fk_accounts_");
        });

        modelBuilder.Entity<AttendanceHoliday>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("attendance_holiday_pkey");

            entity.ToTable("attendance_holiday");

            entity.HasIndex(e => new { e.Date, e.IsActive }, "attendance__date_3ffd05_idx");

            entity.HasIndex(e => e.CreatedById, "attendance_holiday_created_by_id_752fc2d0");

            entity.HasIndex(e => e.Date, "attendance_holiday_date_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.CreatedById).HasColumnName("created_by_id");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.Title)
                .HasMaxLength(120)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(d => d.CreatedBy).WithMany(p => p.AttendanceHolidays)
                .HasForeignKey(d => d.CreatedById)
                .HasConstraintName("attendance_holiday_created_by_id_752fc2d0_fk_accounts_");
        });

        modelBuilder.Entity<AttendanceQrcode>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("attendance_qrcode_pkey");

            entity.ToTable("attendance_qrcode");

            entity.HasIndex(e => e.Code, "attendance_qrcode_code_d9ecc3f5_like").HasOperators(new[] { "varchar_pattern_ops" });

            entity.HasIndex(e => e.Code, "attendance_qrcode_code_key").IsUnique();

            entity.HasIndex(e => e.CreatedById, "attendance_qrcode_created_by_id_8fb5d10a");

            entity.HasIndex(e => e.Token, "attendance_qrcode_token_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(255)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.CreatedById).HasColumnName("created_by_id");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.ExpiryTimestamp).HasColumnName("expiry_timestamp");
            entity.Property(e => e.GenerationMethod)
                .HasMaxLength(20)
                .HasColumnName("generation_method");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.IsExpired).HasColumnName("is_expired");
            entity.Property(e => e.QrHash).HasColumnName("qr_hash");
            entity.Property(e => e.Token).HasColumnName("token");
            entity.Property(e => e.ValidDate).HasColumnName("valid_date");

            entity.HasOne(d => d.CreatedBy).WithMany(p => p.AttendanceQrcodes)
                .HasForeignKey(d => d.CreatedById)
                .HasConstraintName("attendance_qrcode_created_by_id_8fb5d10a_fk_accounts_");
        });

        modelBuilder.Entity<AuthGroup>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("auth_group_pkey");

            entity.ToTable("auth_group");

            entity.HasIndex(e => e.Name, "auth_group_name_a6ea08ec_like").HasOperators(new[] { "varchar_pattern_ops" });

            entity.HasIndex(e => e.Name, "auth_group_name_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(150)
                .HasColumnName("name");
        });

        modelBuilder.Entity<AuthGroupPermission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("auth_group_permissions_pkey");

            entity.ToTable("auth_group_permissions");

            entity.HasIndex(e => e.GroupId, "auth_group_permissions_group_id_b120cbf9");

            entity.HasIndex(e => new { e.GroupId, e.PermissionId }, "auth_group_permissions_group_id_permission_id_0cd325b0_uniq").IsUnique();

            entity.HasIndex(e => e.PermissionId, "auth_group_permissions_permission_id_84c5c92e");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.PermissionId).HasColumnName("permission_id");

            entity.HasOne(d => d.Group).WithMany(p => p.AuthGroupPermissions)
                .HasForeignKey(d => d.GroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("auth_group_permissions_group_id_b120cbf9_fk_auth_group_id");

            entity.HasOne(d => d.Permission).WithMany(p => p.AuthGroupPermissions)
                .HasForeignKey(d => d.PermissionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("auth_group_permissio_permission_id_84c5c92e_fk_auth_perm");
        });

        modelBuilder.Entity<AuthPermission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("auth_permission_pkey");

            entity.ToTable("auth_permission");

            entity.HasIndex(e => e.ContentTypeId, "auth_permission_content_type_id_2f476e4b");

            entity.HasIndex(e => new { e.ContentTypeId, e.Codename }, "auth_permission_content_type_id_codename_01ab375a_uniq").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Codename)
                .HasMaxLength(100)
                .HasColumnName("codename");
            entity.Property(e => e.ContentTypeId).HasColumnName("content_type_id");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");

            entity.HasOne(d => d.ContentType).WithMany(p => p.AuthPermissions)
                .HasForeignKey(d => d.ContentTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("auth_permission_content_type_id_2f476e4b_fk_django_co");
        });

        modelBuilder.Entity<CoreActivitylog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("core_activitylog_pkey");

            entity.ToTable("core_activitylog");

            entity.HasIndex(e => e.AdminId, "core_activitylog_admin_id_6073fa99");

            entity.HasIndex(e => e.UserId, "core_activitylog_user_id_8705e516");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Action)
                .HasMaxLength(255)
                .HasColumnName("action");
            entity.Property(e => e.AdminId).HasColumnName("admin_id");
            entity.Property(e => e.Details)
                .HasColumnType("jsonb")
                .HasColumnName("details");
            entity.Property(e => e.IpAddress).HasColumnName("ip_address");
            entity.Property(e => e.Timestamp).HasColumnName("timestamp");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Admin).WithMany(p => p.CoreActivitylogs)
                .HasForeignKey(d => d.AdminId)
                .HasConstraintName("core_activitylog_admin_id_6073fa99_fk_accounts_adminuser_id");

            entity.HasOne(d => d.User).WithMany(p => p.CoreActivitylogs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("core_activitylog_user_id_8705e516_fk_accounts_customuser_id");
        });

        modelBuilder.Entity<CoreGlobalsetting>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("core_globalsetting_pkey");

            entity.ToTable("core_globalsetting");

            entity.HasIndex(e => e.Key, "core_globalsetting_key_50b930ca_like").HasOperators(new[] { "varchar_pattern_ops" });

            entity.HasIndex(e => e.Key, "core_globalsetting_key_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Key)
                .HasMaxLength(100)
                .HasColumnName("key");
            entity.Property(e => e.Value).HasColumnName("value");
        });

        modelBuilder.Entity<DjangoAdminLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("django_admin_log_pkey");

            entity.ToTable("django_admin_log");

            entity.HasIndex(e => e.ContentTypeId, "django_admin_log_content_type_id_c4bce8eb");

            entity.HasIndex(e => e.UserId, "django_admin_log_user_id_c564eba6");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ActionFlag).HasColumnName("action_flag");
            entity.Property(e => e.ActionTime).HasColumnName("action_time");
            entity.Property(e => e.ChangeMessage).HasColumnName("change_message");
            entity.Property(e => e.ContentTypeId).HasColumnName("content_type_id");
            entity.Property(e => e.ObjectId).HasColumnName("object_id");
            entity.Property(e => e.ObjectRepr)
                .HasMaxLength(200)
                .HasColumnName("object_repr");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.ContentType).WithMany(p => p.DjangoAdminLogs)
                .HasForeignKey(d => d.ContentTypeId)
                .HasConstraintName("django_admin_log_content_type_id_c4bce8eb_fk_django_co");

            entity.HasOne(d => d.User).WithMany(p => p.DjangoAdminLogs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("django_admin_log_user_id_c564eba6_fk_accounts_customuser_id");
        });

        modelBuilder.Entity<DjangoContentType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("django_content_type_pkey");

            entity.ToTable("django_content_type");

            entity.HasIndex(e => new { e.AppLabel, e.Model }, "django_content_type_app_label_model_76bd3d3b_uniq").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AppLabel)
                .HasMaxLength(100)
                .HasColumnName("app_label");
            entity.Property(e => e.Model)
                .HasMaxLength(100)
                .HasColumnName("model");
        });

        modelBuilder.Entity<DjangoMigration>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("django_migrations_pkey");

            entity.ToTable("django_migrations");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.App)
                .HasMaxLength(255)
                .HasColumnName("app");
            entity.Property(e => e.Applied).HasColumnName("applied");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
        });

        modelBuilder.Entity<DjangoSession>(entity =>
        {
            entity.HasKey(e => e.SessionKey).HasName("django_session_pkey");

            entity.ToTable("django_session");

            entity.HasIndex(e => e.ExpireDate, "django_session_expire_date_a5c62663");

            entity.HasIndex(e => e.SessionKey, "django_session_session_key_c0390e0f_like").HasOperators(new[] { "varchar_pattern_ops" });

            entity.Property(e => e.SessionKey)
                .HasMaxLength(40)
                .HasColumnName("session_key");
            entity.Property(e => e.ExpireDate).HasColumnName("expire_date");
            entity.Property(e => e.SessionData).HasColumnName("session_data");
        });

        modelBuilder.Entity<LibraryAchiever>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("library_achiever_pkey");

            entity.ToTable("library_achiever");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Achievement)
                .HasMaxLength(255)
                .HasColumnName("achievement");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.Goal)
                .HasMaxLength(100)
                .HasColumnName("goal");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.IsFeatured).HasColumnName("is_featured");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Order).HasColumnName("order");
            entity.Property(e => e.Photo)
                .HasMaxLength(100)
                .HasColumnName("photo");
            entity.Property(e => e.Year).HasColumnName("year");
        });

        modelBuilder.Entity<LibraryAppconfig>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("library_appconfig_pkey");

            entity.ToTable("library_appconfig");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AllowNonPremiumLibraryInfo).HasColumnName("allow_non_premium_library_info");
            entity.Property(e => e.AllowNonPremiumNotifications).HasColumnName("allow_non_premium_notifications");
            entity.Property(e => e.AllowNonPremiumSliders).HasColumnName("allow_non_premium_sliders");
            entity.Property(e => e.DefaultAllowedStudyMinutes).HasColumnName("default_allowed_study_minutes");
            entity.Property(e => e.ExpiredStudentPermissions)
                .HasColumnType("jsonb")
                .HasColumnName("expired_student_permissions");
            entity.Property(e => e.ExpiryDialogMessage).HasColumnName("expiry_dialog_message");
            entity.Property(e => e.ExpiryDialogTitle)
                .HasMaxLength(200)
                .HasColumnName("expiry_dialog_title");
            entity.Property(e => e.IsPremiumGatingEnabled).HasColumnName("is_premium_gating_enabled");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<LibraryDatabasefile>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("library_databasefile_pkey");

            entity.ToTable("library_databasefile");

            entity.HasIndex(e => e.Name, "library_databasefile_name_c1664fa0_like").HasOperators(new[] { "varchar_pattern_ops" });

            entity.HasIndex(e => e.Name, "library_databasefile_name_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ContentType)
                .HasMaxLength(100)
                .HasColumnName("content_type");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.Data).HasColumnName("data");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
        });

        modelBuilder.Entity<LibraryFacility>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("library_facility_pkey");

            entity.ToTable("library_facility");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IconKey)
                .HasMaxLength(50)
                .HasColumnName("icon_key");
            entity.Property(e => e.Image)
                .HasMaxLength(100)
                .HasColumnName("image");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Order).HasColumnName("order");
        });

        modelBuilder.Entity<LibraryHomeslider>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("library_homeslider_pkey");

            entity.ToTable("library_homeslider");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.Image)
                .HasMaxLength(100)
                .HasColumnName("image");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.LinkUrl)
                .HasMaxLength(500)
                .HasColumnName("link_url");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");
            entity.Property(e => e.Subtitle)
                .HasMaxLength(200)
                .HasColumnName("subtitle");
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .HasColumnName("title");
        });

        modelBuilder.Entity<LibraryLibraryinfo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("library_libraryinfo_pkey");
            entity.ToTable("library_libraryinfo");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.LibraryName).HasMaxLength(255).HasColumnName("library_name");
            entity.Property(e => e.Logo).HasMaxLength(255).HasColumnName("logo");
            entity.Property(e => e.BannerImage).HasMaxLength(255).HasColumnName("banner_image");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.EstablishedYear).HasColumnName("established_year");
            entity.Property(e => e.OwnerName).HasMaxLength(255).HasColumnName("owner_name");
            entity.Property(e => e.ContactNumber).HasMaxLength(50).HasColumnName("contact_number");
            entity.Property(e => e.Email).HasMaxLength(255).HasColumnName("email");
            entity.Property(e => e.Website).HasMaxLength(255).HasColumnName("website");
            entity.Property(e => e.OpeningTime).HasColumnName("opening_time");
            entity.Property(e => e.ClosingTime).HasColumnName("closing_time");
            entity.Property(e => e.WeeklyOff).HasMaxLength(100).HasColumnName("weekly_off");
            entity.Property(e => e.TotalCapacity).HasColumnName("total_capacity");
            entity.Property(e => e.AvailableSeats).HasColumnName("available_seats");
            entity.Property(e => e.AddressLine1).HasMaxLength(255).HasColumnName("address_line1");
            entity.Property(e => e.AddressLine2).HasMaxLength(255).HasColumnName("address_line2");
            entity.Property(e => e.Area).HasMaxLength(100).HasColumnName("area");
            entity.Property(e => e.City).HasMaxLength(100).HasColumnName("city");
            entity.Property(e => e.State).HasMaxLength(100).HasColumnName("state");
            entity.Property(e => e.Country).HasMaxLength(100).HasColumnName("country");
            entity.Property(e => e.PinCode).HasMaxLength(20).HasColumnName("pin_code");
            entity.Property(e => e.Latitude).HasColumnType("numeric(10,6)").HasColumnName("latitude");
            entity.Property(e => e.Longitude).HasColumnType("numeric(10,6)").HasColumnName("longitude");
            entity.Property(e => e.GoogleMapUrl).HasMaxLength(500).HasColumnName("google_map_url");
            
            entity.Property(e => e.Wifi).HasColumnName("wifi");
            entity.Property(e => e.Ac).HasColumnName("ac");
            entity.Property(e => e.Cctv).HasColumnName("cctv");
            entity.Property(e => e.DrinkingWater).HasColumnName("drinking_water");
            entity.Property(e => e.Lockers).HasColumnName("lockers");
            entity.Property(e => e.ChargingPoints).HasColumnName("charging_points");
            entity.Property(e => e.Parking).HasColumnName("parking");
            entity.Property(e => e.ReadingArea).HasColumnName("reading_area");
            entity.Property(e => e.ComputerAccess).HasColumnName("computer_access");
            entity.Property(e => e.Printing).HasColumnName("printing");

            entity.Property(e => e.FacebookUrl).HasMaxLength(255).HasColumnName("facebook_url");
            entity.Property(e => e.InstagramUrl).HasMaxLength(255).HasColumnName("instagram_url");
            entity.Property(e => e.WhatsappNumber).HasMaxLength(50).HasColumnName("whatsapp_number");
            entity.Property(e => e.TelegramUrl).HasMaxLength(255).HasColumnName("telegram_url");
            entity.Property(e => e.YoutubeUrl).HasMaxLength(255).HasColumnName("youtube_url");

            entity.Property(e => e.Tagline).HasColumnName("tagline");
            entity.Property(e => e.Mission).HasColumnName("mission");
            entity.Property(e => e.Vision).HasColumnName("vision");
            entity.Property(e => e.History).HasColumnName("history");
            entity.Property(e => e.WelcomeMessage).HasColumnName("welcome_message");
            entity.Property(e => e.Services).HasColumnName("services");
            entity.Property(e => e.CoursesSupported).HasColumnName("courses_supported");
            entity.Property(e => e.StatisticsDescription).HasColumnName("statistics_description");
            entity.Property(e => e.Faq).HasColumnName("faq");
            entity.Property(e => e.Testimonials).HasColumnName("testimonials");
            entity.Property(e => e.EmergencyContact).HasColumnName("emergency_contact");
            entity.Property(e => e.FooterText).HasColumnName("footer_text");

            entity.Property(e => e.MembershipDetails).HasColumnName("membership_details");
            entity.Property(e => e.RegistrationProcess).HasColumnName("registration_process");
            entity.Property(e => e.RequiredDocuments).HasColumnName("required_documents");
            entity.Property(e => e.MembershipBenefits).HasColumnName("membership_benefits");
            entity.Property(e => e.LibraryRules).HasColumnName("library_rules");

            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<LibraryReview>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("library_review_pkey");

            entity.ToTable("library_review");

            entity.HasIndex(e => e.ApprovedById, "library_review_approved_by_id_9b229f8b");

            entity.HasIndex(e => e.StudentId, "library_review_student_id_b855301a");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ApprovedAt).HasColumnName("approved_at");
            entity.Property(e => e.ApprovedById).HasColumnName("approved_by_id");
            entity.Property(e => e.Comment).HasColumnName("comment");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.IsApproved).HasColumnName("is_approved");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.RejectionReason).HasColumnName("rejection_reason");
            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(d => d.ApprovedBy).WithMany(p => p.LibraryReviews)
                .HasForeignKey(d => d.ApprovedById)
                .HasConstraintName("library_review_approved_by_id_9b229f8b_fk_accounts_adminuser_id");

            entity.HasOne(d => d.Student).WithMany(p => p.LibraryReviews)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("library_review_student_id_b855301a_fk_accounts_customuser_id");
        });

        modelBuilder.Entity<MembershipsMembership>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("memberships_membership_pkey");

            entity.ToTable("memberships_membership");

            entity.HasIndex(e => e.EndDate, "memberships_end_dat_dd07db_idx");

            entity.HasIndex(e => e.CreatedById, "memberships_membership_created_by_id_098854cb");

            entity.HasIndex(e => e.PlanId, "memberships_membership_plan_id_a999e2c0");

            entity.HasIndex(e => e.StudentId, "memberships_membership_student_id_aa07aea9");

            entity.HasIndex(e => new { e.StudentId, e.IsActive }, "memberships_student_85dd7a_idx");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.CreatedById).HasColumnName("created_by_id");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.PlanId).HasColumnName("plan_id");
            entity.Property(e => e.PlanNameSnapshot)
                .HasMaxLength(100)
                .HasColumnName("plan_name_snapshot");
            entity.Property(e => e.PriceSnapshot)
                .HasPrecision(10, 2)
                .HasColumnName("price_snapshot");
            entity.Property(e => e.RenewalCount).HasColumnName("renewal_count");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.StudentId).HasColumnName("student_id");

            entity.HasOne(d => d.CreatedBy).WithMany(p => p.MembershipsMemberships)
                .HasForeignKey(d => d.CreatedById)
                .HasConstraintName("memberships_membersh_created_by_id_098854cb_fk_accounts_");

            entity.HasOne(d => d.Plan).WithMany(p => p.MembershipsMemberships)
                .HasForeignKey(d => d.PlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("memberships_membersh_plan_id_a999e2c0_fk_membershi");

            entity.HasOne(d => d.Student).WithMany(p => p.MembershipsMemberships)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("memberships_membersh_student_id_aa07aea9_fk_accounts_");
        });

        modelBuilder.Entity<MembershipsMembershipplan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("memberships_membershipplan_pkey");

            entity.ToTable("memberships_membershipplan");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Benefits)
                .HasColumnType("jsonb")
                .HasColumnName("benefits");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.DurationDays).HasColumnName("duration_days");
            entity.Property(e => e.DurationMonths).HasColumnName("duration_months");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Price)
                .HasPrecision(10, 2)
                .HasColumnName("price");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<NotificationsAdmininboxnotification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("notifications_admininboxnotification_pkey");

            entity.ToTable("notifications_admininboxnotification");

            entity.HasIndex(e => e.StudentId, "notifications_admininboxnotification_student_id_96823e67");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.IsRead).HasColumnName("is_read");
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.RelatedId)
                .HasMaxLength(100)
                .HasColumnName("related_id");
            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .HasColumnName("title");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .HasColumnName("type");

            entity.HasOne(d => d.Student).WithMany(p => p.NotificationsAdmininboxnotifications)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("notifications_admini_student_id_96823e67_fk_accounts_");
        });

        modelBuilder.Entity<NotificationsDevicetoken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("notifications_devicetoken_pkey");

            entity.ToTable("notifications_devicetoken");

            entity.HasIndex(e => e.StudentId, "notifications_devicetoken_student_id_206fe9a0");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.Token).HasColumnName("token");

            entity.HasOne(d => d.Student).WithMany(p => p.NotificationsDevicetokens)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("notifications_device_student_id_206fe9a0_fk_accounts_");
        });

        modelBuilder.Entity<NotificationsNotification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("notifications_notification_pkey");

            entity.ToTable("notifications_notification");

            entity.HasIndex(e => e.CreatedById, "notifications_notification_created_by_id_44297423");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Audience)
                .HasMaxLength(20)
                .HasColumnName("audience");
            entity.Property(e => e.BackgroundImage)
                .HasMaxLength(100)
                .HasColumnName("background_image");
            entity.Property(e => e.Body).HasColumnName("body");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.CreatedById).HasColumnName("created_by_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.DisplayMode)
                .HasMaxLength(20)
                .HasColumnName("display_mode");
            entity.Property(e => e.EventDate).HasColumnName("event_date");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.FailureCount).HasColumnName("failure_count");
            entity.Property(e => e.GoalFilter)
                .HasMaxLength(50)
                .HasColumnName("goal_filter");
            entity.Property(e => e.Layout)
                .HasMaxLength(20)
                .HasColumnName("layout");
            entity.Property(e => e.LinkButtonText)
                .HasMaxLength(100)
                .HasColumnName("link_button_text");
            entity.Property(e => e.LinkUrl)
                .HasMaxLength(200)
                .HasColumnName("link_url");
            entity.Property(e => e.RecurringTime).HasColumnName("recurring_time");
            entity.Property(e => e.ScheduledAt).HasColumnName("scheduled_at");
            entity.Property(e => e.SendEmail).HasColumnName("send_email");
            entity.Property(e => e.SendPush).HasColumnName("send_push");
            entity.Property(e => e.SendSms).HasColumnName("send_sms");
            entity.Property(e => e.SentAt).HasColumnName("sent_at");
            entity.Property(e => e.StatusFilter)
                .HasMaxLength(20)
                .HasColumnName("status_filter");
            entity.Property(e => e.Subtitle)
                .HasMaxLength(300)
                .HasColumnName("subtitle");
            entity.Property(e => e.SuccessCount).HasColumnName("success_count");
            entity.Property(e => e.Target)
                .HasMaxLength(20)
                .HasColumnName("target");
            entity.Property(e => e.TargetGroup)
                .HasMaxLength(20)
                .HasColumnName("target_group");
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .HasColumnName("title");
            entity.Property(e => e.TotalRecipients).HasColumnName("total_recipients");
            entity.Property(e => e.Type)
                .HasMaxLength(30)
                .HasColumnName("type");

            entity.HasOne(d => d.CreatedBy).WithMany(p => p.NotificationsNotifications)
                .HasForeignKey(d => d.CreatedById)
                .HasConstraintName("notifications_notifi_created_by_id_44297423_fk_accounts_");
        });

        modelBuilder.Entity<NotificationsNotificationimage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("notifications_notificationimage_pkey");

            entity.ToTable("notifications_notificationimage");

            entity.HasIndex(e => e.NotificationId, "notifications_notificationimage_notification_id_433e3be0");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Image)
                .HasMaxLength(100)
                .HasColumnName("image");
            entity.Property(e => e.NotificationId).HasColumnName("notification_id");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");

            entity.HasOne(d => d.Notification).WithMany(p => p.NotificationsNotificationimages)
                .HasForeignKey(d => d.NotificationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("notifications_notifi_notification_id_433e3be0_fk_notificat");
        });

        modelBuilder.Entity<NotificationsStudentnotification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("notifications_studentnotification_pkey");

            entity.ToTable("notifications_studentnotification");

            entity.HasIndex(e => e.NotificationId, "notifications_studentnotification_notification_id_c183c280");

            entity.HasIndex(e => e.StudentId, "notifications_studentnotification_student_id_269c68ed");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DeliveredAt).HasColumnName("delivered_at");
            entity.Property(e => e.EmailDelivered).HasColumnName("email_delivered");
            entity.Property(e => e.IsRead).HasColumnName("is_read");
            entity.Property(e => e.NotificationId).HasColumnName("notification_id");
            entity.Property(e => e.PushDelivered).HasColumnName("push_delivered");
            entity.Property(e => e.ReadAt).HasColumnName("read_at");
            entity.Property(e => e.SmsDelivered).HasColumnName("sms_delivered");
            entity.Property(e => e.StudentId).HasColumnName("student_id");

            entity.HasOne(d => d.Notification).WithMany(p => p.NotificationsStudentnotifications)
                .HasForeignKey(d => d.NotificationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("notifications_studen_notification_id_c183c280_fk_notificat");

            entity.HasOne(d => d.Student).WithMany(p => p.NotificationsStudentnotifications)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("notifications_studen_student_id_269c68ed_fk_accounts_");
        });

        modelBuilder.Entity<PaymentsPayment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("payments_payment_pkey");

            entity.ToTable("payments_payment");

            entity.HasIndex(e => e.PaymentDate, "payments_pa_payment_1d6e55_idx");

            entity.HasIndex(e => e.Status, "payments_pa_status_7ad4af_idx");

            entity.HasIndex(e => new { e.StudentId, e.Status }, "payments_pa_student_2663d2_idx");

            entity.HasIndex(e => e.MembershipId, "payments_payment_membership_id_68ea25d2");

            entity.HasIndex(e => e.PaymentId, "payments_payment_payment_id_5ab18190_like").HasOperators(new[] { "varchar_pattern_ops" });

            entity.HasIndex(e => e.PaymentId, "payments_payment_payment_id_key").IsUnique();

            entity.HasIndex(e => e.RecordedById, "payments_payment_recorded_by_id_11b293af");

            entity.HasIndex(e => e.StudentId, "payments_payment_student_id_b5fab56a");

            entity.HasIndex(e => e.VerifiedById, "payments_payment_verified_by_id_d4a4b387");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasPrecision(10, 2)
                .HasColumnName("amount");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.MembershipId).HasColumnName("membership_id");
            entity.Property(e => e.Method)
                .HasMaxLength(20)
                .HasColumnName("method");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.PaidAt).HasColumnName("paid_at");
            entity.Property(e => e.PaymentDate).HasColumnName("payment_date");
            entity.Property(e => e.PaymentId)
                .HasMaxLength(30)
                .HasColumnName("payment_id");
            entity.Property(e => e.PaymentMode)
                .HasMaxLength(20)
                .HasColumnName("payment_mode");
            entity.Property(e => e.ReceiptUrl)
                .HasMaxLength(100)
                .HasColumnName("receipt_url");
            entity.Property(e => e.RecordedById).HasColumnName("recorded_by_id");
            entity.Property(e => e.RefundAmount)
                .HasPrecision(10, 2)
                .HasColumnName("refund_amount");
            entity.Property(e => e.RefundReason).HasColumnName("refund_reason");
            entity.Property(e => e.RefundedAt).HasColumnName("refunded_at");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.TransactionId)
                .HasMaxLength(100)
                .HasColumnName("transaction_id");
            entity.Property(e => e.TransactionRef)
                .HasMaxLength(100)
                .HasColumnName("transaction_ref");
            entity.Property(e => e.VerifiedAt).HasColumnName("verified_at");
            entity.Property(e => e.VerifiedById).HasColumnName("verified_by_id");

            entity.HasOne(d => d.Membership).WithMany(p => p.PaymentsPayments)
                .HasForeignKey(d => d.MembershipId)
                .HasConstraintName("payments_payment_membership_id_68ea25d2_fk_membershi");

            entity.HasOne(d => d.RecordedBy).WithMany(p => p.PaymentsPaymentRecordedBies)
                .HasForeignKey(d => d.RecordedById)
                .HasConstraintName("payments_payment_recorded_by_id_11b293af_fk_accounts_");

            entity.HasOne(d => d.Student).WithMany(p => p.PaymentsPayments)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("payments_payment_student_id_b5fab56a_fk_accounts_customuser_id");

            entity.HasOne(d => d.VerifiedBy).WithMany(p => p.PaymentsPaymentVerifiedBies)
                .HasForeignKey(d => d.VerifiedById)
                .HasConstraintName("payments_payment_verified_by_id_d4a4b387_fk_accounts_");
        });

        modelBuilder.Entity<SeatsFloor>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("seats_floor_pkey");

            entity.ToTable("seats_floor");

            entity.HasIndex(e => e.Name, "seats_floor_name_cba6808a_like").HasOperators(new[] { "varchar_pattern_ops" });

            entity.HasIndex(e => e.Name, "seats_floor_name_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
            entity.Property(e => e.Order).HasColumnName("order");
        });

        modelBuilder.Entity<SeatsSeat>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("seats_seat_pkey");

            entity.ToTable("seats_seat");

            entity.HasIndex(e => e.AssignedById, "seats_seat_assigned_by_id_d98ab468");

            entity.HasIndex(e => new { e.Floor, e.Row, e.SeatNumber }, "seats_seat_floor_row_seat_number_a2e25ffa_uniq").IsUnique();

            entity.HasIndex(e => e.RowRefId, "seats_seat_row_ref_id_9cd08093");

            entity.HasIndex(e => e.StudentId, "seats_seat_student_id_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AssignedAt).HasColumnName("assigned_at");
            entity.Property(e => e.AssignedById).HasColumnName("assigned_by_id");
            entity.Property(e => e.Floor)
                .HasMaxLength(50)
                .HasColumnName("floor");
            entity.Property(e => e.IsReservedForGirls).HasColumnName("is_reserved_for_girls");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.Row)
                .HasMaxLength(10)
                .HasColumnName("row");
            entity.Property(e => e.RowRefId).HasColumnName("row_ref_id");
            entity.Property(e => e.SeatNumber)
                .HasMaxLength(10)
                .HasColumnName("seat_number");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.StudentId).HasColumnName("student_id");

            entity.HasOne(d => d.AssignedBy).WithMany(p => p.SeatsSeats)
                .HasForeignKey(d => d.AssignedById)
                .HasConstraintName("seats_seat_assigned_by_id_d98ab468_fk_accounts_adminuser_id");

            entity.HasOne(d => d.RowRef).WithMany(p => p.SeatsSeats)
                .HasForeignKey(d => d.RowRefId)
                .HasConstraintName("seats_seat_row_ref_id_9cd08093_fk_seats_seatrow_id");

            entity.HasOne(d => d.Student).WithOne(p => p.SeatsSeat)
                .HasForeignKey<SeatsSeat>(d => d.StudentId)
                .HasConstraintName("seats_seat_student_id_9f4f5265_fk_accounts_customuser_id");
        });

        modelBuilder.Entity<SeatsSeatassignment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("seats_seatassignment_pkey");

            entity.ToTable("seats_seatassignment");

            entity.HasIndex(e => e.SeatId, "seats_seatassignment_seat_id_d5ddd646");

            entity.HasIndex(e => e.StudentId, "seats_seatassignment_student_id_cab95c5c");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AssignedDate).HasColumnName("assigned_date");
            entity.Property(e => e.ReleasedDate).HasColumnName("released_date");
            entity.Property(e => e.SeatId).HasColumnName("seat_id");
            entity.Property(e => e.StudentId).HasColumnName("student_id");

            entity.HasOne(d => d.Seat).WithMany(p => p.SeatsSeatassignments)
                .HasForeignKey(d => d.SeatId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("seats_seatassignment_seat_id_d5ddd646_fk_seats_seat_id");

            entity.HasOne(d => d.Student).WithMany(p => p.SeatsSeatassignments)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("seats_seatassignment_student_id_cab95c5c_fk_accounts_");
        });

        modelBuilder.Entity<SeatsSeatchangelog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("seats_seatchangelog_pkey");

            entity.ToTable("seats_seatchangelog");

            entity.HasIndex(e => e.ChangedById, "seats_seatchangelog_changed_by_id_cdd2a8f0");

            entity.HasIndex(e => e.PreviousSeatId, "seats_seatchangelog_previous_seat_id_322f1f6b");

            entity.HasIndex(e => e.SeatId, "seats_seatchangelog_seat_id_d31cdd48");

            entity.HasIndex(e => e.StudentId, "seats_seatchangelog_student_id_75b8adc9");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Action)
                .HasMaxLength(30)
                .HasColumnName("action");
            entity.Property(e => e.ChangedAt).HasColumnName("changed_at");
            entity.Property(e => e.ChangedById).HasColumnName("changed_by_id");
            entity.Property(e => e.PreviousSeatId).HasColumnName("previous_seat_id");
            entity.Property(e => e.Reason).HasColumnName("reason");
            entity.Property(e => e.SeatId).HasColumnName("seat_id");
            entity.Property(e => e.StudentId).HasColumnName("student_id");

            entity.HasOne(d => d.ChangedBy).WithMany(p => p.SeatsSeatchangelogs)
                .HasForeignKey(d => d.ChangedById)
                .HasConstraintName("seats_seatchangelog_changed_by_id_cdd2a8f0_fk_accounts_");

            entity.HasOne(d => d.PreviousSeat).WithMany(p => p.SeatsSeatchangelogPreviousSeats)
                .HasForeignKey(d => d.PreviousSeatId)
                .HasConstraintName("seats_seatchangelog_previous_seat_id_322f1f6b_fk_seats_seat_id");

            entity.HasOne(d => d.Seat).WithMany(p => p.SeatsSeatchangelogSeats)
                .HasForeignKey(d => d.SeatId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("seats_seatchangelog_seat_id_d31cdd48_fk_seats_seat_id");

            entity.HasOne(d => d.Student).WithMany(p => p.SeatsSeatchangelogs)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("seats_seatchangelog_student_id_75b8adc9_fk_accounts_");
        });

        modelBuilder.Entity<SeatsSeatrow>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("seats_seatrow_pkey");

            entity.ToTable("seats_seatrow");

            entity.HasIndex(e => e.FloorId, "seats_seatrow_floor_id_d4f49e54");

            entity.HasIndex(e => new { e.FloorId, e.Label }, "seats_seatrow_floor_id_label_289d368e_uniq").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FloorId).HasColumnName("floor_id");
            entity.Property(e => e.Label)
                .HasMaxLength(10)
                .HasColumnName("label");
            entity.Property(e => e.Order).HasColumnName("order");

            entity.HasOne(d => d.Floor).WithMany(p => p.SeatsSeatrows)
                .HasForeignKey(d => d.FloorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("seats_seatrow_floor_id_d4f49e54_fk_seats_floor_id");
        });

        modelBuilder.Entity<StudentsReferralcode>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("students_referralcode_pkey");

            entity.ToTable("students_referralcode");

            entity.HasIndex(e => e.Code, "students_referralcode_code_066e8579_like").HasOperators(new[] { "varchar_pattern_ops" });

            entity.HasIndex(e => e.Code, "students_referralcode_code_key").IsUnique();

            entity.HasIndex(e => e.StudentId, "students_referralcode_student_id_ca5fc2fe");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BenefitGiven)
                .HasMaxLength(255)
                .HasColumnName("benefit_given");
            entity.Property(e => e.Code)
                .HasMaxLength(20)
                .HasColumnName("code");
            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.UsedByCount).HasColumnName("used_by_count");

            entity.HasOne(d => d.Student).WithMany(p => p.StudentsReferralcodes)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("students_referralcod_student_id_ca5fc2fe_fk_accounts_");
        });

        modelBuilder.Entity<StudentsReferralhistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("students_referralhistory_pkey");

            entity.ToTable("students_referralhistory");

            entity.HasIndex(e => e.ReferredStudentId, "students_referralhistory_referred_student_id_9f6f3db5");

            entity.HasIndex(e => e.ReferrerId, "students_referralhistory_referrer_id_bb28ff49");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AppliedAt).HasColumnName("applied_at");
            entity.Property(e => e.ReferredStudentId).HasColumnName("referred_student_id");
            entity.Property(e => e.ReferrerId).HasColumnName("referrer_id");

            entity.HasOne(d => d.ReferredStudent).WithMany(p => p.StudentsReferralhistoryReferredStudents)
                .HasForeignKey(d => d.ReferredStudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("students_referralhis_referred_student_id_9f6f3db5_fk_accounts_");

            entity.HasOne(d => d.Referrer).WithMany(p => p.StudentsReferralhistoryReferrers)
                .HasForeignKey(d => d.ReferrerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("students_referralhis_referrer_id_bb28ff49_fk_accounts_");
        });

        modelBuilder.Entity<StudentsStudentprofile>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("students_studentprofile_pkey");

            entity.ToTable("students_studentprofile");

            entity.HasIndex(e => e.CreatedAt, "students_st_created_f5606a_idx");

            entity.HasIndex(e => e.Goal, "students_st_goal_72b058_idx");

            entity.HasIndex(e => e.Status, "students_st_status_6012f4_idx");

            entity.HasIndex(e => e.StudentId, "students_st_student_58f688_idx");

            entity.HasIndex(e => e.ReferredById, "students_studentprofile_referred_by_id_65c9f295");

            entity.HasIndex(e => e.StudentId, "students_studentprofile_student_id_4817d513_like").HasOperators(new[] { "varchar_pattern_ops" });

            entity.HasIndex(e => e.StudentId, "students_studentprofile_student_id_key").IsUnique();

            entity.HasIndex(e => e.SuspendedById, "students_studentprofile_suspended_by_id_2f16f156");

            entity.HasIndex(e => e.UserId, "students_studentprofile_user_id_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.AllowedStudyMinutes).HasColumnName("allowed_study_minutes");
            entity.Property(e => e.Caste)
                .HasMaxLength(50)
                .HasColumnName("caste");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.Dob).HasColumnName("dob");
            entity.Property(e => e.Gender)
                .HasMaxLength(20)
                .HasColumnName("gender");
            entity.Property(e => e.Goal)
                .HasMaxLength(50)
                .HasColumnName("goal");
            entity.Property(e => e.JoiningDate).HasColumnName("joining_date");
            entity.Property(e => e.MiddleName)
                .HasMaxLength(100)
                .HasColumnName("middle_name");
            entity.Property(e => e.ParentMobile)
                .HasMaxLength(15)
                .HasColumnName("parent_mobile");
            entity.Property(e => e.PreferredLanguage)
                .HasMaxLength(10)
                .HasColumnName("preferred_language");
            entity.Property(e => e.ProfilePhoto)
                .HasMaxLength(100)
                .HasColumnName("profile_photo");
            entity.Property(e => e.ReferredById).HasColumnName("referred_by_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.StudentId)
                .HasMaxLength(20)
                .HasColumnName("student_id");
            entity.Property(e => e.SuspendedAt).HasColumnName("suspended_at");
            entity.Property(e => e.SuspendedById).HasColumnName("suspended_by_id");
            entity.Property(e => e.SuspensionReason).HasColumnName("suspension_reason");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.ReferredBy).WithMany(p => p.InverseReferredBy)
                .HasForeignKey(d => d.ReferredById)
                .HasConstraintName("students_studentprof_referred_by_id_65c9f295_fk_students_");

            entity.HasOne(d => d.SuspendedBy).WithMany(p => p.StudentsStudentprofiles)
                .HasForeignKey(d => d.SuspendedById)
                .HasConstraintName("students_studentprof_suspended_by_id_2f16f156_fk_accounts_");

            entity.HasOne(d => d.User).WithOne(p => p.StudentsStudentprofile)
                .HasForeignKey<StudentsStudentprofile>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("students_studentprof_user_id_43a83eee_fk_accounts_");
        });

        modelBuilder.Entity<StudyStudysession>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("study_studysession_pkey");

            entity.ToTable("study_studysession");

            entity.HasIndex(e => e.StudentId, "study_studysession_student_id_4613795f");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DurationMinutes).HasColumnName("duration_minutes");
            entity.Property(e => e.EndTime).HasColumnName("end_time");
            entity.Property(e => e.PausedMinutes).HasColumnName("paused_minutes");
            entity.Property(e => e.StartTime).HasColumnName("start_time");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.StudentId).HasColumnName("student_id");

            entity.HasOne(d => d.Student).WithMany(p => p.StudyStudysessions)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("study_studysession_student_id_4613795f_fk_accounts_");
        });

        modelBuilder.Entity<TokenBlacklistBlacklistedtoken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("token_blacklist_blacklistedtoken_pkey");

            entity.ToTable("token_blacklist_blacklistedtoken");

            entity.HasIndex(e => e.TokenId, "token_blacklist_blacklistedtoken_token_id_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BlacklistedAt).HasColumnName("blacklisted_at");
            entity.Property(e => e.TokenId).HasColumnName("token_id");

            entity.HasOne(d => d.Token).WithOne(p => p.TokenBlacklistBlacklistedtoken)
                .HasForeignKey<TokenBlacklistBlacklistedtoken>(d => d.TokenId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("token_blacklist_blacklistedtoken_token_id_3cc7fe56_fk");
        });

        modelBuilder.Entity<TokenBlacklistOutstandingtoken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("token_blacklist_outstandingtoken_pkey");

            entity.ToTable("token_blacklist_outstandingtoken");

            entity.HasIndex(e => e.Jti, "token_blacklist_outstandingtoken_jti_hex_d9bdf6f7_like").HasOperators(new[] { "varchar_pattern_ops" });

            entity.HasIndex(e => e.Jti, "token_blacklist_outstandingtoken_jti_hex_d9bdf6f7_uniq").IsUnique();

            entity.HasIndex(e => e.UserId, "token_blacklist_outstandingtoken_user_id_83bc629a");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.Jti)
                .HasMaxLength(255)
                .HasColumnName("jti");
            entity.Property(e => e.Token).HasColumnName("token");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.TokenBlacklistOutstandingtokens)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("token_blacklist_outs_user_id_83bc629a_fk_accounts_");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
