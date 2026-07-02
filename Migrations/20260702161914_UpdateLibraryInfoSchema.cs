using System;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WebApplication1.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLibraryInfoSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:auth.aal_level", "aal1,aal2,aal3")
                .Annotation("Npgsql:Enum:auth.code_challenge_method", "s256,plain")
                .Annotation("Npgsql:Enum:auth.factor_status", "unverified,verified")
                .Annotation("Npgsql:Enum:auth.factor_type", "totp,webauthn,phone")
                .Annotation("Npgsql:Enum:auth.oauth_authorization_status", "pending,approved,denied,expired")
                .Annotation("Npgsql:Enum:auth.oauth_client_type", "public,confidential")
                .Annotation("Npgsql:Enum:auth.oauth_registration_type", "dynamic,manual")
                .Annotation("Npgsql:Enum:auth.oauth_response_type", "code")
                .Annotation("Npgsql:Enum:auth.one_time_token_type", "confirmation_token,reauthentication_token,recovery_token,email_change_token_new,email_change_token_current,phone_change_token")
                .Annotation("Npgsql:Enum:realtime.action", "INSERT,UPDATE,DELETE,TRUNCATE,ERROR")
                .Annotation("Npgsql:Enum:realtime.equality_op", "eq,neq,lt,lte,gt,gte,in")
                .Annotation("Npgsql:Enum:storage.buckettype", "STANDARD,ANALYTICS,VECTOR")
                .Annotation("Npgsql:PostgresExtension:extensions.pg_stat_statements", ",,")
                .Annotation("Npgsql:PostgresExtension:extensions.pgcrypto", ",,")
                .Annotation("Npgsql:PostgresExtension:extensions.uuid-ossp", ",,")
                .Annotation("Npgsql:PostgresExtension:vault.supabase_vault", ",,");

            migrationBuilder.CreateTable(
                name: "accounts_adminuser",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    username = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    password = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    first_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    last_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    mobile = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    permissions = table.Column<string>(type: "jsonb", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    date_joined = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_login = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    supabase_uid = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by_id = table.Column<long>(type: "bigint", nullable: true),
                    profile_image = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("accounts_adminuser_pkey", x => x.id);
                    table.ForeignKey(
                        name: "accounts_adminuser_created_by_id_02f160a4_fk_accounts_",
                        column: x => x.created_by_id,
                        principalTable: "accounts_adminuser",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "accounts_authtokenrevocation",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    jti = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    user_identifier = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("accounts_authtokenrevocation_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "accounts_customuser",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    password = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    last_login = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_superuser = table.Column<bool>(type: "boolean", nullable: false),
                    username = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    first_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    last_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    is_staff = table.Column<bool>(type: "boolean", nullable: false),
                    date_joined = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    mobile = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    otp = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    otp_expiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    supabase_uid = table.Column<Guid>(type: "uuid", nullable: true),
                    otp_attempts = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("accounts_customuser_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "auth_group",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("auth_group_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "core_globalsetting",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    value = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("core_globalsetting_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "django_content_type",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    app_label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("django_content_type_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "django_migrations",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    app = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    applied = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("django_migrations_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "django_session",
                columns: table => new
                {
                    session_key = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    session_data = table.Column<string>(type: "text", nullable: false),
                    expire_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("django_session_pkey", x => x.session_key);
                });

            migrationBuilder.CreateTable(
                name: "library_achiever",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    photo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    achievement = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    goal = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_featured = table.Column<bool>(type: "boolean", nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("library_achiever_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "library_appconfig",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    is_premium_gating_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    expiry_dialog_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    expiry_dialog_message = table.Column<string>(type: "text", nullable: false),
                    allow_non_premium_notifications = table.Column<bool>(type: "boolean", nullable: false),
                    allow_non_premium_library_info = table.Column<bool>(type: "boolean", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    allow_non_premium_sliders = table.Column<bool>(type: "boolean", nullable: false),
                    default_allowed_study_minutes = table.Column<int>(type: "integer", nullable: false),
                    expired_student_permissions = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("library_appconfig_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "library_databasefile",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    data = table.Column<byte[]>(type: "bytea", nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("library_databasefile_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "library_facility",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    icon_key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    image = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("library_facility_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "library_homeslider",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    subtitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    image = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    link_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("library_homeslider_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "library_libraryinfo",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    library_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    logo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    banner_image = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    description = table.Column<string>(type: "text", nullable: false),
                    established_year = table.Column<int>(type: "integer", nullable: true),
                    owner_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    contact_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    website = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    opening_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    closing_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    weekly_off = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    total_capacity = table.Column<int>(type: "integer", nullable: false),
                    available_seats = table.Column<int>(type: "integer", nullable: false),
                    address_line1 = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    address_line2 = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    area = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    state = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    pin_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    latitude = table.Column<decimal>(type: "numeric(10,6)", nullable: false),
                    longitude = table.Column<decimal>(type: "numeric(10,6)", nullable: false),
                    google_map_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    wifi = table.Column<bool>(type: "boolean", nullable: true),
                    ac = table.Column<bool>(type: "boolean", nullable: true),
                    cctv = table.Column<bool>(type: "boolean", nullable: true),
                    drinking_water = table.Column<bool>(type: "boolean", nullable: true),
                    lockers = table.Column<bool>(type: "boolean", nullable: true),
                    charging_points = table.Column<bool>(type: "boolean", nullable: true),
                    parking = table.Column<bool>(type: "boolean", nullable: true),
                    reading_area = table.Column<bool>(type: "boolean", nullable: true),
                    computer_access = table.Column<bool>(type: "boolean", nullable: true),
                    printing = table.Column<bool>(type: "boolean", nullable: true),
                    facebook_url = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    instagram_url = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    whatsapp_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    telegram_url = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    youtube_url = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("library_libraryinfo_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "memberships_membershipplan",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    duration_months = table.Column<int>(type: "integer", nullable: false),
                    price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    benefits = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    duration_days = table.Column<int>(type: "integer", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("memberships_membershipplan_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "seats_floor",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("seats_floor_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "attendance_holiday",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("attendance_holiday_pkey", x => x.id);
                    table.ForeignKey(
                        name: "attendance_holiday_created_by_id_752fc2d0_fk_accounts_",
                        column: x => x.created_by_id,
                        principalTable: "accounts_adminuser",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "attendance_qrcode",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    valid_date = table.Column<DateOnly>(type: "date", nullable: false),
                    is_expired = table.Column<bool>(type: "boolean", nullable: false),
                    expiry_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    generation_method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    qr_hash = table.Column<string>(type: "text", nullable: false),
                    token = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("attendance_qrcode_pkey", x => x.id);
                    table.ForeignKey(
                        name: "attendance_qrcode_created_by_id_8fb5d10a_fk_accounts_",
                        column: x => x.created_by_id,
                        principalTable: "accounts_adminuser",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "notifications_notification",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    target_group = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<long>(type: "bigint", nullable: true),
                    failure_count = table.Column<int>(type: "integer", nullable: false),
                    goal_filter = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    scheduled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    send_email = table.Column<bool>(type: "boolean", nullable: false),
                    send_push = table.Column<bool>(type: "boolean", nullable: false),
                    send_sms = table.Column<bool>(type: "boolean", nullable: false),
                    status_filter = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    success_count = table.Column<int>(type: "integer", nullable: false),
                    target = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    total_recipients = table.Column<int>(type: "integer", nullable: false),
                    audience = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    background_image = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    description = table.Column<string>(type: "text", nullable: false),
                    display_mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    event_date = table.Column<DateOnly>(type: "date", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    layout = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    link_button_text = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    link_url = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    recurring_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    subtitle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("notifications_notification_pkey", x => x.id);
                    table.ForeignKey(
                        name: "notifications_notifi_created_by_id_44297423_fk_accounts_",
                        column: x => x.created_by_id,
                        principalTable: "accounts_adminuser",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "core_activitylog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    action = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ip_address = table.Column<IPAddress>(type: "inet", nullable: true),
                    details = table.Column<string>(type: "jsonb", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: true),
                    admin_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("core_activitylog_pkey", x => x.id);
                    table.ForeignKey(
                        name: "core_activitylog_admin_id_6073fa99_fk_accounts_adminuser_id",
                        column: x => x.admin_id,
                        principalTable: "accounts_adminuser",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "core_activitylog_user_id_8705e516_fk_accounts_customuser_id",
                        column: x => x.user_id,
                        principalTable: "accounts_customuser",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "library_review",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    comment = table.Column<string>(type: "text", nullable: false),
                    is_approved = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    student_id = table.Column<long>(type: "bigint", nullable: false),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approved_by_id = table.Column<long>(type: "bigint", nullable: true),
                    rejection_reason = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("library_review_pkey", x => x.id);
                    table.ForeignKey(
                        name: "library_review_approved_by_id_9b229f8b_fk_accounts_adminuser_id",
                        column: x => x.approved_by_id,
                        principalTable: "accounts_adminuser",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "library_review_student_id_b855301a_fk_accounts_customuser_id",
                        column: x => x.student_id,
                        principalTable: "accounts_customuser",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "notifications_admininboxnotification",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    related_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    student_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("notifications_admininboxnotification_pkey", x => x.id);
                    table.ForeignKey(
                        name: "notifications_admini_student_id_96823e67_fk_accounts_",
                        column: x => x.student_id,
                        principalTable: "accounts_customuser",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "notifications_devicetoken",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    token = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    student_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("notifications_devicetoken_pkey", x => x.id);
                    table.ForeignKey(
                        name: "notifications_device_student_id_206fe9a0_fk_accounts_",
                        column: x => x.student_id,
                        principalTable: "accounts_customuser",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "students_referralcode",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    used_by_count = table.Column<int>(type: "integer", nullable: false),
                    benefit_given = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    student_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("students_referralcode_pkey", x => x.id);
                    table.ForeignKey(
                        name: "students_referralcod_student_id_ca5fc2fe_fk_accounts_",
                        column: x => x.student_id,
                        principalTable: "accounts_customuser",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "students_referralhistory",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    applied_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    referred_student_id = table.Column<long>(type: "bigint", nullable: false),
                    referrer_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("students_referralhistory_pkey", x => x.id);
                    table.ForeignKey(
                        name: "students_referralhis_referred_student_id_9f6f3db5_fk_accounts_",
                        column: x => x.referred_student_id,
                        principalTable: "accounts_customuser",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "students_referralhis_referrer_id_bb28ff49_fk_accounts_",
                        column: x => x.referrer_id,
                        principalTable: "accounts_customuser",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "students_studentprofile",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    goal = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    dob = table.Column<DateOnly>(type: "date", nullable: true),
                    caste = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    profile_photo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    parent_mobile = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    gender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    middle_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    preferred_language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    referred_by_id = table.Column<long>(type: "bigint", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    student_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    suspended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    suspended_by_id = table.Column<long>(type: "bigint", nullable: true),
                    suspension_reason = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    allowed_study_minutes = table.Column<int>(type: "integer", nullable: true),
                    joining_date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("students_studentprofile_pkey", x => x.id);
                    table.ForeignKey(
                        name: "students_studentprof_referred_by_id_65c9f295_fk_students_",
                        column: x => x.referred_by_id,
                        principalTable: "students_studentprofile",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "students_studentprof_suspended_by_id_2f16f156_fk_accounts_",
                        column: x => x.suspended_by_id,
                        principalTable: "accounts_adminuser",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "students_studentprof_user_id_43a83eee_fk_accounts_",
                        column: x => x.user_id,
                        principalTable: "accounts_customuser",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "study_studysession",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    start_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    student_id = table.Column<long>(type: "bigint", nullable: false),
                    paused_minutes = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("study_studysession_pkey", x => x.id);
                    table.ForeignKey(
                        name: "study_studysession_student_id_4613795f_fk_accounts_",
                        column: x => x.student_id,
                        principalTable: "accounts_customuser",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "token_blacklist_outstandingtoken",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    token = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: true),
                    jti = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("token_blacklist_outstandingtoken_pkey", x => x.id);
                    table.ForeignKey(
                        name: "token_blacklist_outs_user_id_83bc629a_fk_accounts_",
                        column: x => x.user_id,
                        principalTable: "accounts_customuser",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "accounts_customuser_groups",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customuser_id = table.Column<long>(type: "bigint", nullable: false),
                    group_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("accounts_customuser_groups_pkey", x => x.id);
                    table.ForeignKey(
                        name: "accounts_customuser__customuser_id_bc55088e_fk_accounts_",
                        column: x => x.customuser_id,
                        principalTable: "accounts_customuser",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "accounts_customuser_groups_group_id_86ba5f9e_fk_auth_group_id",
                        column: x => x.group_id,
                        principalTable: "auth_group",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "auth_permission",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_type_id = table.Column<int>(type: "integer", nullable: false),
                    codename = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("auth_permission_pkey", x => x.id);
                    table.ForeignKey(
                        name: "auth_permission_content_type_id_2f476e4b_fk_django_co",
                        column: x => x.content_type_id,
                        principalTable: "django_content_type",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "django_admin_log",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    action_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    object_id = table.Column<string>(type: "text", nullable: true),
                    object_repr = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    action_flag = table.Column<short>(type: "smallint", nullable: false),
                    change_message = table.Column<string>(type: "text", nullable: false),
                    content_type_id = table.Column<int>(type: "integer", nullable: true),
                    user_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("django_admin_log_pkey", x => x.id);
                    table.ForeignKey(
                        name: "django_admin_log_content_type_id_c4bce8eb_fk_django_co",
                        column: x => x.content_type_id,
                        principalTable: "django_content_type",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "django_admin_log_user_id_c564eba6_fk_accounts_customuser_id",
                        column: x => x.user_id,
                        principalTable: "accounts_customuser",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "memberships_membership",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    student_id = table.Column<long>(type: "bigint", nullable: false),
                    plan_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    plan_name_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    price_snapshot = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    renewal_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("memberships_membership_pkey", x => x.id);
                    table.ForeignKey(
                        name: "memberships_membersh_created_by_id_098854cb_fk_accounts_",
                        column: x => x.created_by_id,
                        principalTable: "accounts_adminuser",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "memberships_membersh_plan_id_a999e2c0_fk_membershi",
                        column: x => x.plan_id,
                        principalTable: "memberships_membershipplan",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "memberships_membersh_student_id_aa07aea9_fk_accounts_",
                        column: x => x.student_id,
                        principalTable: "accounts_customuser",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "seats_seatrow",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    label = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    floor_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("seats_seatrow_pkey", x => x.id);
                    table.ForeignKey(
                        name: "seats_seatrow_floor_id_d4f49e54_fk_seats_floor_id",
                        column: x => x.floor_id,
                        principalTable: "seats_floor",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "attendance_attendance",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    time_in = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    is_manual = table.Column<bool>(type: "boolean", nullable: false),
                    student_id = table.Column<long>(type: "bigint", nullable: false),
                    qr_code_id = table.Column<long>(type: "bigint", nullable: true),
                    is_present = table.Column<bool>(type: "boolean", nullable: false),
                    marked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    marked_by_id = table.Column<long>(type: "bigint", nullable: true),
                    method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    late_mark = table.Column<bool>(type: "boolean", nullable: false),
                    time_out = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    total_hours = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    under_time = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("attendance_attendance_pkey", x => x.id);
                    table.ForeignKey(
                        name: "attendance_attendanc_marked_by_id_0698c76f_fk_accounts_",
                        column: x => x.marked_by_id,
                        principalTable: "accounts_adminuser",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "attendance_attendanc_qr_code_id_e23eb1f8_fk_attendanc",
                        column: x => x.qr_code_id,
                        principalTable: "attendance_qrcode",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "attendance_attendanc_student_id_94863613_fk_accounts_",
                        column: x => x.student_id,
                        principalTable: "accounts_customuser",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "notifications_notificationimage",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    image = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    notification_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("notifications_notificationimage_pkey", x => x.id);
                    table.ForeignKey(
                        name: "notifications_notifi_notification_id_433e3be0_fk_notificat",
                        column: x => x.notification_id,
                        principalTable: "notifications_notification",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "notifications_studentnotification",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    notification_id = table.Column<long>(type: "bigint", nullable: false),
                    student_id = table.Column<long>(type: "bigint", nullable: false),
                    delivered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    email_delivered = table.Column<bool>(type: "boolean", nullable: false),
                    push_delivered = table.Column<bool>(type: "boolean", nullable: false),
                    sms_delivered = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("notifications_studentnotification_pkey", x => x.id);
                    table.ForeignKey(
                        name: "notifications_studen_notification_id_c183c280_fk_notificat",
                        column: x => x.notification_id,
                        principalTable: "notifications_notification",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "notifications_studen_student_id_269c68ed_fk_accounts_",
                        column: x => x.student_id,
                        principalTable: "accounts_customuser",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "token_blacklist_blacklistedtoken",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    blacklisted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    token_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("token_blacklist_blacklistedtoken_pkey", x => x.id);
                    table.ForeignKey(
                        name: "token_blacklist_blacklistedtoken_token_id_3cc7fe56_fk",
                        column: x => x.token_id,
                        principalTable: "token_blacklist_outstandingtoken",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "accounts_customuser_user_permissions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customuser_id = table.Column<long>(type: "bigint", nullable: false),
                    permission_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("accounts_customuser_user_permissions_pkey", x => x.id);
                    table.ForeignKey(
                        name: "accounts_customuser__customuser_id_0deaefae_fk_accounts_",
                        column: x => x.customuser_id,
                        principalTable: "accounts_customuser",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "accounts_customuser__permission_id_aea3d0e5_fk_auth_perm",
                        column: x => x.permission_id,
                        principalTable: "auth_permission",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "auth_group_permissions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    group_id = table.Column<int>(type: "integer", nullable: false),
                    permission_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("auth_group_permissions_pkey", x => x.id);
                    table.ForeignKey(
                        name: "auth_group_permissio_permission_id_84c5c92e_fk_auth_perm",
                        column: x => x.permission_id,
                        principalTable: "auth_permission",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "auth_group_permissions_group_id_b120cbf9_fk_auth_group_id",
                        column: x => x.group_id,
                        principalTable: "auth_group",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "payments_payment",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    payment_mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    payment_date = table.Column<DateOnly>(type: "date", nullable: false),
                    transaction_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    receipt_url = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    membership_id = table.Column<long>(type: "bigint", nullable: true),
                    student_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    payment_id = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    recorded_by_id = table.Column<long>(type: "bigint", nullable: true),
                    refund_amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    refund_reason = table.Column<string>(type: "text", nullable: true),
                    refunded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    transaction_ref = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    verified_by_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("payments_payment_pkey", x => x.id);
                    table.ForeignKey(
                        name: "payments_payment_membership_id_68ea25d2_fk_membershi",
                        column: x => x.membership_id,
                        principalTable: "memberships_membership",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "payments_payment_recorded_by_id_11b293af_fk_accounts_",
                        column: x => x.recorded_by_id,
                        principalTable: "accounts_adminuser",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "payments_payment_student_id_b5fab56a_fk_accounts_customuser_id",
                        column: x => x.student_id,
                        principalTable: "accounts_customuser",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "payments_payment_verified_by_id_d4a4b387_fk_accounts_",
                        column: x => x.verified_by_id,
                        principalTable: "accounts_adminuser",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "seats_seat",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    floor = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    row = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    seat_number = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    assigned_by_id = table.Column<long>(type: "bigint", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    student_id = table.Column<long>(type: "bigint", nullable: true),
                    row_ref_id = table.Column<long>(type: "bigint", nullable: true),
                    is_reserved_for_girls = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("seats_seat_pkey", x => x.id);
                    table.ForeignKey(
                        name: "seats_seat_assigned_by_id_d98ab468_fk_accounts_adminuser_id",
                        column: x => x.assigned_by_id,
                        principalTable: "accounts_adminuser",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "seats_seat_row_ref_id_9cd08093_fk_seats_seatrow_id",
                        column: x => x.row_ref_id,
                        principalTable: "seats_seatrow",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "seats_seat_student_id_9f4f5265_fk_accounts_customuser_id",
                        column: x => x.student_id,
                        principalTable: "accounts_customuser",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "seats_seatassignment",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    assigned_date = table.Column<DateOnly>(type: "date", nullable: false),
                    released_date = table.Column<DateOnly>(type: "date", nullable: true),
                    seat_id = table.Column<long>(type: "bigint", nullable: false),
                    student_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("seats_seatassignment_pkey", x => x.id);
                    table.ForeignKey(
                        name: "seats_seatassignment_seat_id_d5ddd646_fk_seats_seat_id",
                        column: x => x.seat_id,
                        principalTable: "seats_seat",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "seats_seatassignment_student_id_cab95c5c_fk_accounts_",
                        column: x => x.student_id,
                        principalTable: "accounts_customuser",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "seats_seatchangelog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    action = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    changed_by_id = table.Column<long>(type: "bigint", nullable: true),
                    previous_seat_id = table.Column<long>(type: "bigint", nullable: true),
                    seat_id = table.Column<long>(type: "bigint", nullable: false),
                    student_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("seats_seatchangelog_pkey", x => x.id);
                    table.ForeignKey(
                        name: "seats_seatchangelog_changed_by_id_cdd2a8f0_fk_accounts_",
                        column: x => x.changed_by_id,
                        principalTable: "accounts_adminuser",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "seats_seatchangelog_previous_seat_id_322f1f6b_fk_seats_seat_id",
                        column: x => x.previous_seat_id,
                        principalTable: "seats_seat",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "seats_seatchangelog_seat_id_d31cdd48_fk_seats_seat_id",
                        column: x => x.seat_id,
                        principalTable: "seats_seat",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "seats_seatchangelog_student_id_75b8adc9_fk_accounts_",
                        column: x => x.student_id,
                        principalTable: "accounts_customuser",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "accounts_adminuser_created_by_id_02f160a4",
                table: "accounts_adminuser",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "accounts_adminuser_email_5110578e_like",
                table: "accounts_adminuser",
                column: "email")
                .Annotation("Npgsql:IndexOperators", new[] { "varchar_pattern_ops" });

            migrationBuilder.CreateIndex(
                name: "accounts_adminuser_email_key",
                table: "accounts_adminuser",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "accounts_adminuser_mobile_3d5b9327_like",
                table: "accounts_adminuser",
                column: "mobile")
                .Annotation("Npgsql:IndexOperators", new[] { "varchar_pattern_ops" });

            migrationBuilder.CreateIndex(
                name: "accounts_adminuser_mobile_key",
                table: "accounts_adminuser",
                column: "mobile",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "accounts_adminuser_supabase_uid_key",
                table: "accounts_adminuser",
                column: "supabase_uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "accounts_adminuser_username_4d9b2ca6_like",
                table: "accounts_adminuser",
                column: "username")
                .Annotation("Npgsql:IndexOperators", new[] { "varchar_pattern_ops" });

            migrationBuilder.CreateIndex(
                name: "accounts_adminuser_username_key",
                table: "accounts_adminuser",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "accounts_au_expires_474335_idx",
                table: "accounts_authtokenrevocation",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "accounts_authtokenrevocation_jti_53823217",
                table: "accounts_authtokenrevocation",
                column: "jti");

            migrationBuilder.CreateIndex(
                name: "accounts_authtokenrevocation_jti_53823217_like",
                table: "accounts_authtokenrevocation",
                column: "jti")
                .Annotation("Npgsql:IndexOperators", new[] { "varchar_pattern_ops" });

            migrationBuilder.CreateIndex(
                name: "accounts_authtokenrevocation_token_hash_69826360_like",
                table: "accounts_authtokenrevocation",
                column: "token_hash")
                .Annotation("Npgsql:IndexOperators", new[] { "varchar_pattern_ops" });

            migrationBuilder.CreateIndex(
                name: "accounts_authtokenrevocation_token_hash_key",
                table: "accounts_authtokenrevocation",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "accounts_customuser_email_4fd8e7ce_like",
                table: "accounts_customuser",
                column: "email")
                .Annotation("Npgsql:IndexOperators", new[] { "varchar_pattern_ops" });

            migrationBuilder.CreateIndex(
                name: "accounts_customuser_email_key",
                table: "accounts_customuser",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "accounts_customuser_mobile_a211a2ea_like",
                table: "accounts_customuser",
                column: "mobile")
                .Annotation("Npgsql:IndexOperators", new[] { "varchar_pattern_ops" });

            migrationBuilder.CreateIndex(
                name: "accounts_customuser_mobile_key",
                table: "accounts_customuser",
                column: "mobile",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "accounts_customuser_supabase_uid_key",
                table: "accounts_customuser",
                column: "supabase_uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "accounts_customuser_username_722f3555_like",
                table: "accounts_customuser",
                column: "username")
                .Annotation("Npgsql:IndexOperators", new[] { "varchar_pattern_ops" });

            migrationBuilder.CreateIndex(
                name: "accounts_customuser_username_key",
                table: "accounts_customuser",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "accounts_customuser_groups_customuser_id_bc55088e",
                table: "accounts_customuser_groups",
                column: "customuser_id");

            migrationBuilder.CreateIndex(
                name: "accounts_customuser_groups_customuser_id_group_id_c074bdcb_uniq",
                table: "accounts_customuser_groups",
                columns: new[] { "customuser_id", "group_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "accounts_customuser_groups_group_id_86ba5f9e",
                table: "accounts_customuser_groups",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "accounts_customuser_user_customuser_id_permission_9632a709_uniq",
                table: "accounts_customuser_user_permissions",
                columns: new[] { "customuser_id", "permission_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "accounts_customuser_user_permissions_customuser_id_0deaefae",
                table: "accounts_customuser_user_permissions",
                column: "customuser_id");

            migrationBuilder.CreateIndex(
                name: "accounts_customuser_user_permissions_permission_id_aea3d0e5",
                table: "accounts_customuser_user_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "attendance__date_61f2e1_idx",
                table: "attendance_attendance",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "attendance__is_pres_772c00_idx",
                table: "attendance_attendance",
                column: "is_present");

            migrationBuilder.CreateIndex(
                name: "attendance__student_76a8d7_idx",
                table: "attendance_attendance",
                columns: new[] { "student_id", "date" });

            migrationBuilder.CreateIndex(
                name: "attendance_attendance_marked_by_id_0698c76f",
                table: "attendance_attendance",
                column: "marked_by_id");

            migrationBuilder.CreateIndex(
                name: "attendance_attendance_qr_code_id_e23eb1f8",
                table: "attendance_attendance",
                column: "qr_code_id");

            migrationBuilder.CreateIndex(
                name: "attendance_attendance_student_id_94863613",
                table: "attendance_attendance",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "attendance_attendance_student_id_date_167892e4_uniq",
                table: "attendance_attendance",
                columns: new[] { "student_id", "date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "attendance__date_3ffd05_idx",
                table: "attendance_holiday",
                columns: new[] { "date", "is_active" });

            migrationBuilder.CreateIndex(
                name: "attendance_holiday_created_by_id_752fc2d0",
                table: "attendance_holiday",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "attendance_holiday_date_key",
                table: "attendance_holiday",
                column: "date",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "attendance_qrcode_code_d9ecc3f5_like",
                table: "attendance_qrcode",
                column: "code")
                .Annotation("Npgsql:IndexOperators", new[] { "varchar_pattern_ops" });

            migrationBuilder.CreateIndex(
                name: "attendance_qrcode_code_key",
                table: "attendance_qrcode",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "attendance_qrcode_created_by_id_8fb5d10a",
                table: "attendance_qrcode",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "attendance_qrcode_token_key",
                table: "attendance_qrcode",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "auth_group_name_a6ea08ec_like",
                table: "auth_group",
                column: "name")
                .Annotation("Npgsql:IndexOperators", new[] { "varchar_pattern_ops" });

            migrationBuilder.CreateIndex(
                name: "auth_group_name_key",
                table: "auth_group",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "auth_group_permissions_group_id_b120cbf9",
                table: "auth_group_permissions",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "auth_group_permissions_group_id_permission_id_0cd325b0_uniq",
                table: "auth_group_permissions",
                columns: new[] { "group_id", "permission_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "auth_group_permissions_permission_id_84c5c92e",
                table: "auth_group_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "auth_permission_content_type_id_2f476e4b",
                table: "auth_permission",
                column: "content_type_id");

            migrationBuilder.CreateIndex(
                name: "auth_permission_content_type_id_codename_01ab375a_uniq",
                table: "auth_permission",
                columns: new[] { "content_type_id", "codename" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "core_activitylog_admin_id_6073fa99",
                table: "core_activitylog",
                column: "admin_id");

            migrationBuilder.CreateIndex(
                name: "core_activitylog_user_id_8705e516",
                table: "core_activitylog",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "core_globalsetting_key_50b930ca_like",
                table: "core_globalsetting",
                column: "key")
                .Annotation("Npgsql:IndexOperators", new[] { "varchar_pattern_ops" });

            migrationBuilder.CreateIndex(
                name: "core_globalsetting_key_key",
                table: "core_globalsetting",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "django_admin_log_content_type_id_c4bce8eb",
                table: "django_admin_log",
                column: "content_type_id");

            migrationBuilder.CreateIndex(
                name: "django_admin_log_user_id_c564eba6",
                table: "django_admin_log",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "django_content_type_app_label_model_76bd3d3b_uniq",
                table: "django_content_type",
                columns: new[] { "app_label", "model" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "django_session_expire_date_a5c62663",
                table: "django_session",
                column: "expire_date");

            migrationBuilder.CreateIndex(
                name: "django_session_session_key_c0390e0f_like",
                table: "django_session",
                column: "session_key")
                .Annotation("Npgsql:IndexOperators", new[] { "varchar_pattern_ops" });

            migrationBuilder.CreateIndex(
                name: "library_databasefile_name_c1664fa0_like",
                table: "library_databasefile",
                column: "name")
                .Annotation("Npgsql:IndexOperators", new[] { "varchar_pattern_ops" });

            migrationBuilder.CreateIndex(
                name: "library_databasefile_name_key",
                table: "library_databasefile",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "library_review_approved_by_id_9b229f8b",
                table: "library_review",
                column: "approved_by_id");

            migrationBuilder.CreateIndex(
                name: "library_review_student_id_b855301a",
                table: "library_review",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "memberships_end_dat_dd07db_idx",
                table: "memberships_membership",
                column: "end_date");

            migrationBuilder.CreateIndex(
                name: "memberships_membership_created_by_id_098854cb",
                table: "memberships_membership",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "memberships_membership_plan_id_a999e2c0",
                table: "memberships_membership",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "memberships_membership_student_id_aa07aea9",
                table: "memberships_membership",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "memberships_student_85dd7a_idx",
                table: "memberships_membership",
                columns: new[] { "student_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "notifications_admininboxnotification_student_id_96823e67",
                table: "notifications_admininboxnotification",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "notifications_devicetoken_student_id_206fe9a0",
                table: "notifications_devicetoken",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "notifications_notification_created_by_id_44297423",
                table: "notifications_notification",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "notifications_notificationimage_notification_id_433e3be0",
                table: "notifications_notificationimage",
                column: "notification_id");

            migrationBuilder.CreateIndex(
                name: "notifications_studentnotification_notification_id_c183c280",
                table: "notifications_studentnotification",
                column: "notification_id");

            migrationBuilder.CreateIndex(
                name: "notifications_studentnotification_student_id_269c68ed",
                table: "notifications_studentnotification",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "payments_pa_payment_1d6e55_idx",
                table: "payments_payment",
                column: "payment_date");

            migrationBuilder.CreateIndex(
                name: "payments_pa_status_7ad4af_idx",
                table: "payments_payment",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "payments_pa_student_2663d2_idx",
                table: "payments_payment",
                columns: new[] { "student_id", "status" });

            migrationBuilder.CreateIndex(
                name: "payments_payment_membership_id_68ea25d2",
                table: "payments_payment",
                column: "membership_id");

            migrationBuilder.CreateIndex(
                name: "payments_payment_payment_id_5ab18190_like",
                table: "payments_payment",
                column: "payment_id")
                .Annotation("Npgsql:IndexOperators", new[] { "varchar_pattern_ops" });

            migrationBuilder.CreateIndex(
                name: "payments_payment_payment_id_key",
                table: "payments_payment",
                column: "payment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "payments_payment_recorded_by_id_11b293af",
                table: "payments_payment",
                column: "recorded_by_id");

            migrationBuilder.CreateIndex(
                name: "payments_payment_student_id_b5fab56a",
                table: "payments_payment",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "payments_payment_verified_by_id_d4a4b387",
                table: "payments_payment",
                column: "verified_by_id");

            migrationBuilder.CreateIndex(
                name: "seats_floor_name_cba6808a_like",
                table: "seats_floor",
                column: "name")
                .Annotation("Npgsql:IndexOperators", new[] { "varchar_pattern_ops" });

            migrationBuilder.CreateIndex(
                name: "seats_floor_name_key",
                table: "seats_floor",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "seats_seat_assigned_by_id_d98ab468",
                table: "seats_seat",
                column: "assigned_by_id");

            migrationBuilder.CreateIndex(
                name: "seats_seat_floor_row_seat_number_a2e25ffa_uniq",
                table: "seats_seat",
                columns: new[] { "floor", "row", "seat_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "seats_seat_row_ref_id_9cd08093",
                table: "seats_seat",
                column: "row_ref_id");

            migrationBuilder.CreateIndex(
                name: "seats_seat_student_id_key",
                table: "seats_seat",
                column: "student_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "seats_seatassignment_seat_id_d5ddd646",
                table: "seats_seatassignment",
                column: "seat_id");

            migrationBuilder.CreateIndex(
                name: "seats_seatassignment_student_id_cab95c5c",
                table: "seats_seatassignment",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "seats_seatchangelog_changed_by_id_cdd2a8f0",
                table: "seats_seatchangelog",
                column: "changed_by_id");

            migrationBuilder.CreateIndex(
                name: "seats_seatchangelog_previous_seat_id_322f1f6b",
                table: "seats_seatchangelog",
                column: "previous_seat_id");

            migrationBuilder.CreateIndex(
                name: "seats_seatchangelog_seat_id_d31cdd48",
                table: "seats_seatchangelog",
                column: "seat_id");

            migrationBuilder.CreateIndex(
                name: "seats_seatchangelog_student_id_75b8adc9",
                table: "seats_seatchangelog",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "seats_seatrow_floor_id_d4f49e54",
                table: "seats_seatrow",
                column: "floor_id");

            migrationBuilder.CreateIndex(
                name: "seats_seatrow_floor_id_label_289d368e_uniq",
                table: "seats_seatrow",
                columns: new[] { "floor_id", "label" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "students_referralcode_code_066e8579_like",
                table: "students_referralcode",
                column: "code")
                .Annotation("Npgsql:IndexOperators", new[] { "varchar_pattern_ops" });

            migrationBuilder.CreateIndex(
                name: "students_referralcode_code_key",
                table: "students_referralcode",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "students_referralcode_student_id_ca5fc2fe",
                table: "students_referralcode",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "students_referralhistory_referred_student_id_9f6f3db5",
                table: "students_referralhistory",
                column: "referred_student_id");

            migrationBuilder.CreateIndex(
                name: "students_referralhistory_referrer_id_bb28ff49",
                table: "students_referralhistory",
                column: "referrer_id");

            migrationBuilder.CreateIndex(
                name: "students_st_created_f5606a_idx",
                table: "students_studentprofile",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "students_st_goal_72b058_idx",
                table: "students_studentprofile",
                column: "goal");

            migrationBuilder.CreateIndex(
                name: "students_st_status_6012f4_idx",
                table: "students_studentprofile",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "students_st_student_58f688_idx",
                table: "students_studentprofile",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "students_studentprofile_referred_by_id_65c9f295",
                table: "students_studentprofile",
                column: "referred_by_id");

            migrationBuilder.CreateIndex(
                name: "students_studentprofile_student_id_4817d513_like",
                table: "students_studentprofile",
                column: "student_id")
                .Annotation("Npgsql:IndexOperators", new[] { "varchar_pattern_ops" });

            migrationBuilder.CreateIndex(
                name: "students_studentprofile_student_id_key",
                table: "students_studentprofile",
                column: "student_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "students_studentprofile_suspended_by_id_2f16f156",
                table: "students_studentprofile",
                column: "suspended_by_id");

            migrationBuilder.CreateIndex(
                name: "students_studentprofile_user_id_key",
                table: "students_studentprofile",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "study_studysession_student_id_4613795f",
                table: "study_studysession",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "token_blacklist_blacklistedtoken_token_id_key",
                table: "token_blacklist_blacklistedtoken",
                column: "token_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "token_blacklist_outstandingtoken_jti_hex_d9bdf6f7_like",
                table: "token_blacklist_outstandingtoken",
                column: "jti")
                .Annotation("Npgsql:IndexOperators", new[] { "varchar_pattern_ops" });

            migrationBuilder.CreateIndex(
                name: "token_blacklist_outstandingtoken_jti_hex_d9bdf6f7_uniq",
                table: "token_blacklist_outstandingtoken",
                column: "jti",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "token_blacklist_outstandingtoken_user_id_83bc629a",
                table: "token_blacklist_outstandingtoken",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "accounts_authtokenrevocation");

            migrationBuilder.DropTable(
                name: "accounts_customuser_groups");

            migrationBuilder.DropTable(
                name: "accounts_customuser_user_permissions");

            migrationBuilder.DropTable(
                name: "attendance_attendance");

            migrationBuilder.DropTable(
                name: "attendance_holiday");

            migrationBuilder.DropTable(
                name: "auth_group_permissions");

            migrationBuilder.DropTable(
                name: "core_activitylog");

            migrationBuilder.DropTable(
                name: "core_globalsetting");

            migrationBuilder.DropTable(
                name: "django_admin_log");

            migrationBuilder.DropTable(
                name: "django_migrations");

            migrationBuilder.DropTable(
                name: "django_session");

            migrationBuilder.DropTable(
                name: "library_achiever");

            migrationBuilder.DropTable(
                name: "library_appconfig");

            migrationBuilder.DropTable(
                name: "library_databasefile");

            migrationBuilder.DropTable(
                name: "library_facility");

            migrationBuilder.DropTable(
                name: "library_homeslider");

            migrationBuilder.DropTable(
                name: "library_libraryinfo");

            migrationBuilder.DropTable(
                name: "library_review");

            migrationBuilder.DropTable(
                name: "notifications_admininboxnotification");

            migrationBuilder.DropTable(
                name: "notifications_devicetoken");

            migrationBuilder.DropTable(
                name: "notifications_notificationimage");

            migrationBuilder.DropTable(
                name: "notifications_studentnotification");

            migrationBuilder.DropTable(
                name: "payments_payment");

            migrationBuilder.DropTable(
                name: "seats_seatassignment");

            migrationBuilder.DropTable(
                name: "seats_seatchangelog");

            migrationBuilder.DropTable(
                name: "students_referralcode");

            migrationBuilder.DropTable(
                name: "students_referralhistory");

            migrationBuilder.DropTable(
                name: "students_studentprofile");

            migrationBuilder.DropTable(
                name: "study_studysession");

            migrationBuilder.DropTable(
                name: "token_blacklist_blacklistedtoken");

            migrationBuilder.DropTable(
                name: "attendance_qrcode");

            migrationBuilder.DropTable(
                name: "auth_permission");

            migrationBuilder.DropTable(
                name: "auth_group");

            migrationBuilder.DropTable(
                name: "notifications_notification");

            migrationBuilder.DropTable(
                name: "memberships_membership");

            migrationBuilder.DropTable(
                name: "seats_seat");

            migrationBuilder.DropTable(
                name: "token_blacklist_outstandingtoken");

            migrationBuilder.DropTable(
                name: "django_content_type");

            migrationBuilder.DropTable(
                name: "memberships_membershipplan");

            migrationBuilder.DropTable(
                name: "accounts_adminuser");

            migrationBuilder.DropTable(
                name: "seats_seatrow");

            migrationBuilder.DropTable(
                name: "accounts_customuser");

            migrationBuilder.DropTable(
                name: "seats_floor");
        }
    }
}
