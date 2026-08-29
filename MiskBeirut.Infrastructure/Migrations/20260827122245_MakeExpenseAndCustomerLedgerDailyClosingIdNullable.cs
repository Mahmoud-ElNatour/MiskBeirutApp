using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiskBeirut.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeExpenseAndCustomerLedgerDailyClosingIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "DailyClosingId",
                schema: "backoffice",
                table: "expenses",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<bool>(
                name: "IsManualEntry",
                schema: "backoffice",
                table: "expenses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "DailyClosingId",
                schema: "customer",
                table: "customer_ledger",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<bool>(
                name: "IsManualEntry",
                schema: "customer",
                table: "customer_ledger",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsManualEntry",
                schema: "backoffice",
                table: "expenses");

            migrationBuilder.DropColumn(
                name: "IsManualEntry",
                schema: "customer",
                table: "customer_ledger");

            migrationBuilder.AlterColumn<int>(
                name: "DailyClosingId",
                schema: "backoffice",
                table: "expenses",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "DailyClosingId",
                schema: "customer",
                table: "customer_ledger",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
