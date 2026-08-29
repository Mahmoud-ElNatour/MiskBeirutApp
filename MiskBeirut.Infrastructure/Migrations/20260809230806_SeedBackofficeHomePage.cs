using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiskBeirut.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedBackofficeHomePage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Explicit Ids: these are the first two rows in a brand-new table, and AdminHome's
            // attributes below need a stable PageId to reference within this same migration.
            migrationBuilder.InsertData(
                schema: "backoffice",
                table: "pages",
                columns: new[] { "Id", "PageName" },
                values: new object[,]
                {
                    { 1, "Global" },
                    { 2, "AdminHome" }
                });

            migrationBuilder.InsertData(
                schema: "backoffice",
                table: "pageattributes",
                columns: new[] { "PageId", "AttributeName", "AttributeType", "Value" },
                values: new object[,]
                {
                    { 2, "hero_eyebrow", "Text", "Welcome Back" },
                    { 2, "hero_title", "Text", "Welcome to the" },
                    { 2, "hero_title_highlight", "Text", "MiskBeirut Management System" },
                    { 2, "hero_subtitle", "Text", "Streamline your business operations with our integrated management tools. Manage your team, track your customers, and settle your books with ease." },
                    { 2, "control_panel_title", "Text", "Control Panel" },
                    { 2, "control_panel_body", "RichText", "The central hub for your business administration. Manage all modules including <strong>Employees</strong>, <strong>Customers</strong>, and detailed configurations from one centralized console." },
                    { 2, "control_panel_cta_label", "Text", "Go to Control Panel" },
                    { 2, "daily_close_title", "Text", "Daily Close" },
                    { 2, "daily_close_body", "Text", "Settle daily financial receipts, document sales figures, calculate cashouts, and record daily expenses. Export printable summaries for records." },
                    { 2, "daily_close_cta_label", "Text", "Manage Daily Close" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM [backoffice].[pageattributes] WHERE [PageId] = 2;");
            migrationBuilder.Sql("DELETE FROM [backoffice].[pages] WHERE [Id] IN (1, 2);");
        }
    }
}
