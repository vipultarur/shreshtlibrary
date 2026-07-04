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
            // The columns were already renamed and dropped manually in the Supabase database
            // via the startup script, so executing these RenameColumn commands will crash the app
            // because the old column names (e.g., "Vision" with capital V) no longer exist.
            // Leaving this empty simply marks the migration as applied in __EFMigrationsHistory.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Empty down migration
        }
    }
}
