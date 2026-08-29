using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiskBeirut.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBackofficePages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pages",
                schema: "backoffice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PageName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "pageattributes",
                schema: "backoffice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PageId = table.Column<int>(type: "int", nullable: false),
                    AttributeName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AttributeType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Text"),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pageattributes", x => x.Id);
                    table.CheckConstraint("CK_pageattributes_type", "[AttributeType] IN ('Text', 'RichText', 'Image', 'Link', 'Video', 'Number', 'Date', 'Boolean')");
                    table.ForeignKey(
                        name: "FK_pageattributes_pages_PageId",
                        column: x => x.PageId,
                        principalSchema: "backoffice",
                        principalTable: "pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UQ_pageattributes_PageAttr",
                schema: "backoffice",
                table: "pageattributes",
                columns: new[] { "PageId", "AttributeName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pages_PageName",
                schema: "backoffice",
                table: "pages",
                column: "PageName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pageattributes",
                schema: "backoffice");

            migrationBuilder.DropTable(
                name: "pages",
                schema: "backoffice");
        }
    }
}
