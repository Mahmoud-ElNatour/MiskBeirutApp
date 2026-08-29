using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiskBeirut.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeWorkingBaseSalary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BaseSalary",
                schema: "backoffice",
                table: "employee_working",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            // Backfill: this column never existed before, so there's no prior human-entered value to
            // clobber — seeding every existing row from its employee's *current* Base Salary is the
            // only sensible default. This is a best-effort approximation for old records (the actual
            // rate in effect back when they were created may have differed) — new rows created from
            // here on get their own accurate snapshot at creation time (see EmployeeWorking.BaseSalary).
            migrationBuilder.Sql("""
                UPDATE ew SET ew.BaseSalary = e.BaseSalary
                FROM [backoffice].[employee_working] ew
                JOIN [backoffice].[employees] e ON e.Id = ew.EmployeeId;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaseSalary",
                schema: "backoffice",
                table: "employee_working");
        }
    }
}
