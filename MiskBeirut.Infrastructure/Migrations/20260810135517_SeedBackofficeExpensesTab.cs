using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiskBeirut.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedBackofficeExpensesTab : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "backoffice",
                table: "pageattributes",
                columns: new[] { "PageId", "AttributeName", "AttributeType", "Value" },
                values: new object[,]
                {
                    // ControlPanel (Id 7) card
                    { 7, "expenses_title", "Text", "Expenses" },
                    { 7, "expenses_body", "Text", "Track and review business expenses across every receiver." },

                    // Expenses page (Id 14) empty state
                    { 14, "empty_title", "Text", "No expenses found" },
                    { 14, "empty_body", "Text", "Adjust your filters to see data here." }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM [backoffice].[pageattributes] WHERE [PageId] = 7 AND [AttributeName] IN (N'expenses_title', N'expenses_body');");
            migrationBuilder.Sql("DELETE FROM [backoffice].[pageattributes] WHERE [PageId] = 14 AND [AttributeName] IN (N'empty_title', N'empty_body');");
        }
    }
}
