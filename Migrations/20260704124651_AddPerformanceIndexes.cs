using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication1.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex("IX_accounts_customuser_mobile",
                "accounts_customuser", "mobile");
            migrationBuilder.CreateIndex("IX_accounts_customuser_email",
                "accounts_customuser", "email");
            migrationBuilder.CreateIndex("IX_attendance_attendance_student_date",
                "attendance_attendance", new[] { "student_id", "date" });
            migrationBuilder.CreateIndex("IX_students_referralcode_code",
                "students_referralcode", "code");
            migrationBuilder.CreateIndex("IX_notifications_devicetoken_token",
                "notifications_devicetoken", "token");
            migrationBuilder.CreateIndex("IX_authtokenrevocation_tokenhash",
                "accounts_authtokenrevocation", "token_hash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex("IX_accounts_customuser_mobile", "accounts_customuser");
            migrationBuilder.DropIndex("IX_accounts_customuser_email", "accounts_customuser");
            migrationBuilder.DropIndex("IX_attendance_attendance_student_date", "attendance_attendance");
            migrationBuilder.DropIndex("IX_students_referralcode_code", "students_referralcode");
            migrationBuilder.DropIndex("IX_notifications_devicetoken_token", "notifications_devicetoken");
            migrationBuilder.DropIndex("IX_authtokenrevocation_tokenhash", "accounts_authtokenrevocation");
        }
    }
}
