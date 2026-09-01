using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiskBeirut.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddArabicVacancyAndReasonCopy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DepartmentAr",
                schema: "customer",
                table: "vacancies",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmploymentTypeAr",
                schema: "customer",
                table: "vacancies",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocationAr",
                schema: "customer",
                table: "vacancies",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TitleAr",
                schema: "customer",
                table: "vacancies",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameAr",
                schema: "customer",
                table: "inquiry_reasons",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            // Translations for the rows seeded by SeedVacancies / SeedInquiryReasons. Matched on
            // Slug/Name rather than Id so this lands correctly whatever identity values the target
            // database ended up with, and skips anything an editor has already translated by hand.
            migrationBuilder.Sql(@"
UPDATE [customer].[vacancies] SET [TitleAr] = N'الشيف التنفيذي', [DepartmentAr] = N'المطبخ', [LocationAr] = N'بيروت', [EmploymentTypeAr] = N'دوام كامل' WHERE [Slug] = 'head-chef' AND [TitleAr] IS NULL;
UPDATE [customer].[vacancies] SET [TitleAr] = N'مستشار أغذية ومشروبات', [DepartmentAr] = N'استشارات', [LocationAr] = N'عن بُعد', [EmploymentTypeAr] = N'دوام كامل' WHERE [Slug] = 'fb-consultant' AND [TitleAr] IS NULL;
UPDATE [customer].[vacancies] SET [TitleAr] = N'منسق مناسبات', [DepartmentAr] = N'المناسبات', [LocationAr] = N'بيروت', [EmploymentTypeAr] = N'دوام كامل' WHERE [Slug] = 'events-coord' AND [TitleAr] IS NULL;
UPDATE [customer].[vacancies] SET [TitleAr] = N'مدير المطعم', [DepartmentAr] = N'العمليات', [LocationAr] = N'بيروت', [EmploymentTypeAr] = N'دوام كامل' WHERE [Slug] = 'res-manager' AND [TitleAr] IS NULL;

UPDATE [customer].[inquiry_reasons] SET [NameAr] = N'استشارات' WHERE [Name] = 'Consulting' AND [NameAr] IS NULL;
UPDATE [customer].[inquiry_reasons] SET [NameAr] = N'ملاحظات' WHERE [Name] = 'Feedback' AND [NameAr] IS NULL;
UPDATE [customer].[inquiry_reasons] SET [NameAr] = N'استفسار عام' WHERE [Name] = 'General Inquiry' AND [NameAr] IS NULL;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DepartmentAr",
                schema: "customer",
                table: "vacancies");

            migrationBuilder.DropColumn(
                name: "EmploymentTypeAr",
                schema: "customer",
                table: "vacancies");

            migrationBuilder.DropColumn(
                name: "LocationAr",
                schema: "customer",
                table: "vacancies");

            migrationBuilder.DropColumn(
                name: "TitleAr",
                schema: "customer",
                table: "vacancies");

            migrationBuilder.DropColumn(
                name: "NameAr",
                schema: "customer",
                table: "inquiry_reasons");
        }
    }
}
