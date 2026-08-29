using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiskBeirut.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedVacancies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "customer",
                table: "vacancies",
                columns: new[] { "Slug", "Title", "Department", "Location", "EmploymentType", "Icon", "DisplayOrder" },
                values: new object[,]
                {
                    { "head-chef", "Head Chef", "Culinary", "Beirut", "Full-Time", "restaurant_menu", 1 },
                    { "fb-consultant", "F&B Consultant", "Consulting", "Remote", "Full-Time", "business_center", 2 },
                    { "events-coord", "Events Coordinator", "Events", "Beirut", "Full-Time", "celebration", 3 },
                    { "res-manager", "Restaurant Manager", "Operations", "Beirut", "Full-Time", "apartment", 4 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM [customer].[vacancies] WHERE [Slug] IN ('head-chef', 'fb-consultant', 'events-coord', 'res-manager');");
        }
    }
}
