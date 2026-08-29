using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiskBeirut.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeWorkingUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No DropIndex here: this database was built from a hand-written SQL script (see
            // CLAUDE.md), not a prior EF migration, and it never actually had the single-column
            // IX_employee_working_EmployeeId index the model snapshot assumed — only the primary
            // key. Nothing to drop.
            migrationBuilder.CreateIndex(
                name: "IX_employee_working_EmployeeId_Year_Month",
                schema: "backoffice",
                table: "employee_working",
                columns: new[] { "EmployeeId", "Year", "Month" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Mirrors Up(): this database never had the single-column index, so rolling back just
            // removes the unique composite index and leaves the table as it actually was before.
            migrationBuilder.DropIndex(
                name: "IX_employee_working_EmployeeId_Year_Month",
                schema: "backoffice",
                table: "employee_working");
        }
    }
}
