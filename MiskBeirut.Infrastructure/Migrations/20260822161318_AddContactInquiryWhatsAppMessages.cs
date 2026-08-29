using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiskBeirut.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContactInquiryWhatsAppMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contact_inquiry_whatsapp_messages",
                schema: "customer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContactInquiryId = table.Column<int>(type: "int", nullable: false),
                    ToPhoneNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TemplateName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    ExternalMessageId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SentByUserId = table.Column<int>(type: "int", nullable: true),
                    SentByUsername = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contact_inquiry_whatsapp_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_contact_inquiry_whatsapp_messages_contact_inquiries_ContactInquiryId",
                        column: x => x.ContactInquiryId,
                        principalSchema: "customer",
                        principalTable: "contact_inquiries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_contact_inquiry_whatsapp_messages_ContactInquiryId",
                schema: "customer",
                table: "contact_inquiry_whatsapp_messages",
                column: "ContactInquiryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contact_inquiry_whatsapp_messages",
                schema: "customer");
        }
    }
}
