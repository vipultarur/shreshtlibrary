using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WebApplication1.Migrations
{
    /// <inheritdoc />
    public partial class AddAboutContentAndGallery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoursesSupported",
                table: "library_libraryinfo",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContact",
                table: "library_libraryinfo",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Faq",
                table: "library_libraryinfo",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FooterText",
                table: "library_libraryinfo",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "History",
                table: "library_libraryinfo",
                type: "text",
                nullable: true);

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
                name: "Mission",
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

            migrationBuilder.AddColumn<string>(
                name: "Services",
                table: "library_libraryinfo",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatisticsDescription",
                table: "library_libraryinfo",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tagline",
                table: "library_libraryinfo",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Testimonials",
                table: "library_libraryinfo",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Vision",
                table: "library_libraryinfo",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WelcomeMessage",
                table: "library_libraryinfo",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LibraryGalleryImages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ImageUrl = table.Column<string>(type: "text", nullable: false),
                    Caption = table.Column<string>(type: "text", nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryGalleryImages", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LibraryGalleryImages");

            migrationBuilder.DropColumn(
                name: "CoursesSupported",
                table: "library_libraryinfo");

            migrationBuilder.DropColumn(
                name: "EmergencyContact",
                table: "library_libraryinfo");

            migrationBuilder.DropColumn(
                name: "Faq",
                table: "library_libraryinfo");

            migrationBuilder.DropColumn(
                name: "FooterText",
                table: "library_libraryinfo");

            migrationBuilder.DropColumn(
                name: "History",
                table: "library_libraryinfo");

            migrationBuilder.DropColumn(
                name: "LibraryRules",
                table: "library_libraryinfo");

            migrationBuilder.DropColumn(
                name: "MembershipBenefits",
                table: "library_libraryinfo");

            migrationBuilder.DropColumn(
                name: "MembershipDetails",
                table: "library_libraryinfo");

            migrationBuilder.DropColumn(
                name: "Mission",
                table: "library_libraryinfo");

            migrationBuilder.DropColumn(
                name: "RegistrationProcess",
                table: "library_libraryinfo");

            migrationBuilder.DropColumn(
                name: "RequiredDocuments",
                table: "library_libraryinfo");

            migrationBuilder.DropColumn(
                name: "Services",
                table: "library_libraryinfo");

            migrationBuilder.DropColumn(
                name: "StatisticsDescription",
                table: "library_libraryinfo");

            migrationBuilder.DropColumn(
                name: "Tagline",
                table: "library_libraryinfo");

            migrationBuilder.DropColumn(
                name: "Testimonials",
                table: "library_libraryinfo");

            migrationBuilder.DropColumn(
                name: "Vision",
                table: "library_libraryinfo");

            migrationBuilder.DropColumn(
                name: "WelcomeMessage",
                table: "library_libraryinfo");
        }
    }
}
