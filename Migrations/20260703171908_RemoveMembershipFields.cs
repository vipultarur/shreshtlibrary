using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication1.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMembershipFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "library_rules",
                table: "library_libraryinfo");

            migrationBuilder.DropColumn(
                name: "membership_benefits",
                table: "library_libraryinfo");

            migrationBuilder.DropColumn(
                name: "membership_details",
                table: "library_libraryinfo");

            migrationBuilder.DropColumn(
                name: "registration_process",
                table: "library_libraryinfo");

            migrationBuilder.DropColumn(
                name: "required_documents",
                table: "library_libraryinfo");

            migrationBuilder.RenameColumn(
                name: "Vision",
                table: "library_libraryinfo",
                newName: "vision");

            migrationBuilder.RenameColumn(
                name: "Testimonials",
                table: "library_libraryinfo",
                newName: "testimonials");

            migrationBuilder.RenameColumn(
                name: "Tagline",
                table: "library_libraryinfo",
                newName: "tagline");

            migrationBuilder.RenameColumn(
                name: "Services",
                table: "library_libraryinfo",
                newName: "services");

            migrationBuilder.RenameColumn(
                name: "Mission",
                table: "library_libraryinfo",
                newName: "mission");

            migrationBuilder.RenameColumn(
                name: "History",
                table: "library_libraryinfo",
                newName: "history");

            migrationBuilder.RenameColumn(
                name: "Faq",
                table: "library_libraryinfo",
                newName: "faq");

            migrationBuilder.RenameColumn(
                name: "WelcomeMessage",
                table: "library_libraryinfo",
                newName: "welcome_message");

            migrationBuilder.RenameColumn(
                name: "StatisticsDescription",
                table: "library_libraryinfo",
                newName: "statistics_description");

            migrationBuilder.RenameColumn(
                name: "FooterText",
                table: "library_libraryinfo",
                newName: "footer_text");

            migrationBuilder.RenameColumn(
                name: "EmergencyContact",
                table: "library_libraryinfo",
                newName: "emergency_contact");

            migrationBuilder.RenameColumn(
                name: "CoursesSupported",
                table: "library_libraryinfo",
                newName: "courses_supported");

            migrationBuilder.AddColumn<string>(
                name: "linkedin_url",
                table: "library_libraryinfo",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "twitter_url",
                table: "library_libraryinfo",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "linkedin_url",
                table: "library_libraryinfo");

            migrationBuilder.DropColumn(
                name: "twitter_url",
                table: "library_libraryinfo");

            migrationBuilder.RenameColumn(
                name: "vision",
                table: "library_libraryinfo",
                newName: "Vision");

            migrationBuilder.RenameColumn(
                name: "testimonials",
                table: "library_libraryinfo",
                newName: "Testimonials");

            migrationBuilder.RenameColumn(
                name: "tagline",
                table: "library_libraryinfo",
                newName: "Tagline");

            migrationBuilder.RenameColumn(
                name: "services",
                table: "library_libraryinfo",
                newName: "Services");

            migrationBuilder.RenameColumn(
                name: "mission",
                table: "library_libraryinfo",
                newName: "Mission");

            migrationBuilder.RenameColumn(
                name: "history",
                table: "library_libraryinfo",
                newName: "History");

            migrationBuilder.RenameColumn(
                name: "faq",
                table: "library_libraryinfo",
                newName: "Faq");

            migrationBuilder.RenameColumn(
                name: "welcome_message",
                table: "library_libraryinfo",
                newName: "WelcomeMessage");

            migrationBuilder.RenameColumn(
                name: "statistics_description",
                table: "library_libraryinfo",
                newName: "StatisticsDescription");

            migrationBuilder.RenameColumn(
                name: "footer_text",
                table: "library_libraryinfo",
                newName: "FooterText");

            migrationBuilder.RenameColumn(
                name: "emergency_contact",
                table: "library_libraryinfo",
                newName: "EmergencyContact");

            migrationBuilder.RenameColumn(
                name: "courses_supported",
                table: "library_libraryinfo",
                newName: "CoursesSupported");

            migrationBuilder.AddColumn<string>(
                name: "LibraryRules",
                table: "library_libraryinfo",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MembershipBenefits",
                table: "library_libraryinfo",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MembershipDetails",
                table: "library_libraryinfo",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationProcess",
                table: "library_libraryinfo",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequiredDocuments",
                table: "library_libraryinfo",
                type: "text",
                nullable: true);
        }
    }
}
