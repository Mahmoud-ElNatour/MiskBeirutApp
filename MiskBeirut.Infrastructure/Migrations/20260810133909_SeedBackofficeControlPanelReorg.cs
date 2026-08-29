using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiskBeirut.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedBackofficeControlPanelReorg : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // New attributes on the existing ControlPanel page (Id 7 from SeedBackofficeContentPages)
            // for its reorganized 3-section layout: title/subtitle already exist and are untouched.
            migrationBuilder.InsertData(
                schema: "backoffice",
                table: "pageattributes",
                columns: new[] { "PageId", "AttributeName", "AttributeType", "Value" },
                values: new object[,]
                {
                    { 7, "section1_title", "Text", "Operations" },
                    { 7, "section2_title", "Text", "Investors" },
                    { 7, "section3_title", "Text", "Administration" },
                    { 7, "employees_title", "Text", "Employees" },
                    { 7, "employees_body", "Text", "Manage staff profiles, shifts, and weekly performance schedules." },
                    { 7, "customers_title", "Text", "Customers" },
                    { 7, "customers_body", "Text", "Access loyalty balances, purchase history, and contact database." },
                    { 7, "payroll_title", "Text", "Payroll" },
                    { 7, "payroll_body", "Text", "Review and manage monthly staff salaries." },
                    { 7, "credits_title", "Text", "Credits" },
                    { 7, "credits_body", "Text", "Review customer credit sales across every account." },
                    { 7, "cashbacks_title", "Text", "Cashbacks" },
                    { 7, "cashbacks_body", "Text", "Review cashback payouts made to customers." },
                    { 7, "deductions_advances_title", "Text", "Deductions & Advances" },
                    { 7, "deductions_advances_body", "Text", "Review salary deductions and advance payments issued to staff." },
                    { 7, "investors_title", "Text", "Investors" },
                    { 7, "investors_body", "Text", "Manage investors and drill into each one's expenses by receiver and withdrawal history." },
                    { 7, "reports_title", "Text", "Reports" },
                    { 7, "reports_body", "Text", "Generate financial summaries, operational insights, and export data." },
                    { 7, "users_title", "Text", "Users" },
                    { 7, "users_body", "Text", "Manage system accounts, permissions, and user access levels." }
                });

            // New pages for the new Investors / Credits / Cashbacks / Deductions & Advances screens.
            migrationBuilder.InsertData(
                schema: "backoffice",
                table: "pages",
                columns: new[] { "Id", "PageName" },
                values: new object[,]
                {
                    { 20, "Investors" },
                    { 21, "Credits" },
                    { 22, "Cashbacks" },
                    { 23, "DeductionsAdvances" }
                });

            migrationBuilder.InsertData(
                schema: "backoffice",
                table: "pageattributes",
                columns: new[] { "PageId", "AttributeName", "AttributeType", "Value" },
                values: new object[,]
                {
                    // Investors
                    { 20, "index_title", "Text", "Investors" },
                    { 20, "index_subtitle", "Text", "Track investor withdrawals and their expenses by receiver." },
                    { 20, "add_button_label", "Text", "Add Investor" },
                    { 20, "empty_title", "Text", "No investors found" },
                    { 20, "empty_body", "Text", "Add an investor to start tracking their withdrawals and expenses." },

                    // Credits
                    { 21, "title", "Text", "Credit Records" },
                    { 21, "subtitle", "Text", "View and filter customer credit sales." },
                    { 21, "empty_title", "Text", "No credit records found" },
                    { 21, "empty_body", "Text", "Adjust your filters to see data here." },

                    // Cashbacks
                    { 22, "title", "Text", "Cashback Records" },
                    { 22, "subtitle", "Text", "View and filter customer cashback payouts." },
                    { 22, "empty_title", "Text", "No cashback records found" },
                    { 22, "empty_body", "Text", "Adjust your filters to see data here." },

                    // Deductions & Advances
                    { 23, "title", "Text", "Deductions & Advances" },
                    { 23, "subtitle", "Text", "View and filter employee advances and deductions." },
                    { 23, "empty_advances", "Text", "No advance records found" },
                    { 23, "empty_deductions", "Text", "No deduction records found" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM [backoffice].[pageattributes] WHERE [PageId] = 7 AND [AttributeName] IN (N'section1_title', N'section2_title', N'section3_title', N'employees_title', N'employees_body', N'customers_title', N'customers_body', N'payroll_title', N'payroll_body', N'credits_title', N'credits_body', N'cashbacks_title', N'cashbacks_body', N'deductions_advances_title', N'deductions_advances_body', N'investors_title', N'investors_body', N'reports_title', N'reports_body', N'users_title', N'users_body');");
            migrationBuilder.Sql("DELETE FROM [backoffice].[pageattributes] WHERE [PageId] BETWEEN 20 AND 23;");
            migrationBuilder.Sql("DELETE FROM [backoffice].[pages] WHERE [Id] BETWEEN 20 AND 23;");
        }
    }
}
