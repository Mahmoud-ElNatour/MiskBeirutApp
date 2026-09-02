using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiskBeirut.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVacancyDetailsAndDeadline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApplicationDeadline",
                schema: "customer",
                table: "vacancies",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "customer",
                table: "vacancies",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionAr",
                schema: "customer",
                table: "vacancies",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Requirements",
                schema: "customer",
                table: "vacancies",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequirementsAr",
                schema: "customer",
                table: "vacancies",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApplicationDeadline",
                schema: "customer",
                table: "vacancies");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "customer",
                table: "vacancies");

            migrationBuilder.DropColumn(
                name: "DescriptionAr",
                schema: "customer",
                table: "vacancies");

            migrationBuilder.DropColumn(
                name: "Requirements",
                schema: "customer",
                table: "vacancies");

            migrationBuilder.DropColumn(
                name: "RequirementsAr",
                schema: "customer",
                table: "vacancies");
        }
    }
}
