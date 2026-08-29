using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiskBeirut.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedInquiryReasons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "customer",
                table: "inquiry_reasons",
                columns: new[] { "Name", "DisplayOrder" },
                values: new object[,]
                {
                    { "Consulting", 1 },
                    { "Feedback", 2 },
                    { "General Inquiry", 3 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM [customer].[inquiry_reasons] WHERE [Name] IN ('Consulting', 'Feedback', 'General Inquiry');");
        }
    }
}
