using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiskBeirut.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedBackofficeRoleManagerContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ControlPanel (Id 7) card
            migrationBuilder.InsertData(
                schema: "backoffice",
                table: "pageattributes",
                columns: new[] { "PageId", "AttributeName", "AttributeType", "Value" },
                values: new object[,]
                {
                    { 7, "role_manager_title", "Text", "Role Manager" },
                    { 7, "role_manager_body", "Text", "Create roles and manage which pages and sections each one can access." }
                });

            migrationBuilder.InsertData(
                schema: "backoffice",
                table: "pages",
                columns: new[] { "Id", "PageName" },
                values: new object[,] { { 24, "RoleManager" } });

            migrationBuilder.InsertData(
                schema: "backoffice",
                table: "pageattributes",
                columns: new[] { "PageId", "AttributeName", "AttributeType", "Value" },
                values: new object[,]
                {
                    { 24, "title", "Text", "Role Manager" },
                    { 24, "subtitle", "Text", "Create roles and manage which pages and sections each one can access." }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM [backoffice].[pageattributes] WHERE [PageId] = 7 AND [AttributeName] IN (N'role_manager_title', N'role_manager_body');");
            migrationBuilder.Sql("DELETE FROM [backoffice].[pageattributes] WHERE [PageId] = 24;");
            migrationBuilder.Sql("DELETE FROM [backoffice].[pages] WHERE [Id] = 24;");
        }
    }
}
