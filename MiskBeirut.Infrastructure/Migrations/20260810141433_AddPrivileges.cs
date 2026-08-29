using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiskBeirut.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPrivileges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "privileges",
                schema: "backoffice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsSection = table.Column<bool>(type: "bit", nullable: false),
                    SectionKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_privileges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "role_privileges",
                schema: "backoffice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    PrivilegeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_privileges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_role_privileges_privileges_PrivilegeId",
                        column: x => x.PrivilegeId,
                        principalSchema: "backoffice",
                        principalTable: "privileges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_role_privileges_roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "backoffice",
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_privileges_Key",
                schema: "backoffice",
                table: "privileges",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_role_privileges_PrivilegeId",
                schema: "backoffice",
                table: "role_privileges",
                column: "PrivilegeId");

            migrationBuilder.CreateIndex(
                name: "IX_role_privileges_RoleId_PrivilegeId",
                schema: "backoffice",
                table: "role_privileges",
                columns: new[] { "RoleId", "PrivilegeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "role_privileges",
                schema: "backoffice");

            migrationBuilder.DropTable(
                name: "privileges",
                schema: "backoffice");
        }
    }
}
