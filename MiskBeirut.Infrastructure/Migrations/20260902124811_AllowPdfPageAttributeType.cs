using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiskBeirut.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AllowPdfPageAttributeType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_page_attributes_type",
                schema: "customer",
                table: "page_attributes");

            migrationBuilder.AddCheckConstraint(
                name: "CK_page_attributes_type",
                schema: "customer",
                table: "page_attributes",
                sql: "[AttributeType] IN ('Text', 'RichText', 'Image', 'Link', 'Pdf', 'Video', 'Number', 'Date', 'Boolean')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_page_attributes_type",
                schema: "customer",
                table: "page_attributes");

            migrationBuilder.AddCheckConstraint(
                name: "CK_page_attributes_type",
                schema: "customer",
                table: "page_attributes",
                sql: "[AttributeType] IN ('Text', 'RichText', 'Image', 'Link', 'Video', 'Number', 'Date', 'Boolean')");
        }
    }
}
