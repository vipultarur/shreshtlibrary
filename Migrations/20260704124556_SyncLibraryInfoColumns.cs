using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication1.Migrations
{
    /// <inheritdoc />
    public partial class SyncLibraryInfoColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE library_libraryinfo SET faq = '[]' WHERE faq IS NULL OR btrim(faq) = '';");
            migrationBuilder.Sql("ALTER TABLE library_libraryinfo ALTER COLUMN faq TYPE jsonb USING faq::jsonb;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE library_libraryinfo ALTER COLUMN faq TYPE text USING faq::text;");
        }
    }
}
