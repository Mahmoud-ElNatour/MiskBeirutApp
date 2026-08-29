using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiskBeirut.Infrastructure.Migrations
{
    /// <summary>
    /// Turns off SQL Server's identity value caching for this database, so IDENTITY columns
    /// (AuditLogs.Id, etc.) increment strictly +1 and survive app/server restarts without the
    /// ~1000-value jumps the default ON caching causes. Applies to the *current* database — no
    /// literal DB name needed — so this runs identically on every environment this migration is
    /// applied to (local, staging, MassiveGrid production).
    /// </summary>
    public partial class DisableIdentityCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ALTER DATABASE SCOPED CONFIGURATION isn't allowed inside a transaction — EF wraps
            // every migration in one by default, so this statement must opt out of it.
            migrationBuilder.Sql("ALTER DATABASE SCOPED CONFIGURATION SET IDENTITY_CACHE = OFF;", suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER DATABASE SCOPED CONFIGURATION SET IDENTITY_CACHE = ON;", suppressTransaction: true);
        }
    }
}
