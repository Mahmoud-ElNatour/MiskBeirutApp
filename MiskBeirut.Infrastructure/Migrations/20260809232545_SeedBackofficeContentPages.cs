using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiskBeirut.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedBackofficeContentPages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ids 1-2 (Global, AdminHome) were seeded by SeedBackofficeHomePage. Explicit Ids here
            // so this migration's own attribute rows below can reference a stable PageId.
            migrationBuilder.InsertData(
                schema: "backoffice",
                table: "pages",
                columns: new[] { "Id", "PageName" },
                values: new object[,]
                {
                    { 3, "Customers" },
                    { 4, "Login" },
                    { 5, "Users" },
                    { 6, "AuditLogs" },
                    { 7, "ControlPanel" },
                    { 8, "Reports" },
                    { 9, "SiteGallery" },
                    { 10, "Roles" },
                    { 11, "Logs" },
                    { 12, "Settings" },
                    { 13, "Receivers" },
                    { 14, "Expenses" },
                    { 15, "Employees" },
                    { 16, "Payroll" },
                    { 17, "DailyClosing" },
                    { 18, "DailyClosingCreate" },
                    { 19, "Menu" }
                });

            migrationBuilder.InsertData(
                schema: "backoffice",
                table: "pageattributes",
                columns: new[] { "PageId", "AttributeName", "AttributeType", "Value" },
                values: new object[,]
                {
                    // Customers
                    { 3, "title", "Text", "Customers Management" },
                    { 3, "subtitle", "Text", "Manage customer information and account balances." },
                    { 3, "add_button_label", "Text", "Add Customer" },
                    { 3, "empty_title", "Text", "No customers found" },
                    { 3, "empty_body", "Text", "Click 'Add Customer' to create your first record." },

                    // Login
                    { 4, "title", "Text", "Account Log In" },
                    { 4, "subtitle", "Text", "Access the MiskBeirut management console" },
                    { 4, "submit_label", "Text", "Log In" },
                    { 4, "signup_prompt", "Text", "Don't have an account?" },
                    { 4, "signup_link_label", "Text", "Sign Up" },

                    // Users
                    { 5, "title", "Text", "User Management" },
                    { 5, "empty_title", "Text", "No users found." },

                    // AuditLogs
                    { 6, "title", "Text", "Audit Logs" },
                    { 6, "empty_title", "Text", "No audit logs found matching the selected criteria." },

                    // ControlPanel
                    { 7, "title", "Text", "Control Panel" },
                    { 7, "subtitle", "Text", "Manage all aspects of your business operations from a central dashboard." },

                    // Reports
                    { 8, "title", "Text", "Reports & Analytics" },
                    { 8, "subtitle", "Text", "Business insights and performance metrics." },

                    // SiteGallery
                    { 9, "title", "Text", "Gallery Images" },
                    { 9, "subtitle", "Text", "Upload and manage images shown on the public landing page." },

                    // Roles
                    { 10, "title", "Text", "Roles Management" },
                    { 10, "subtitle", "Text", "Manage system roles and permissions." },
                    { 10, "add_button_label", "Text", "Add Role" },

                    // Logs
                    { 11, "title", "Text", "System Logs" },
                    { 11, "subtitle", "Text", "Track system changes, user activities, and administrative actions." },

                    // Settings
                    { 12, "title", "Text", "Settings" },
                    { 12, "subtitle", "Text", "Manage your account and application preferences." },

                    // Receivers
                    { 13, "title", "Text", "Receivers Management" },
                    { 13, "subtitle", "Text", "Manage and view all payment recipients." },
                    { 13, "add_button_label", "Text", "Add Receiver" },
                    { 13, "empty_title", "Text", "No receivers found" },
                    { 13, "empty_body", "Text", "Register a new receiver to start tracking payments." },

                    // Expenses
                    { 14, "title", "Text", "Expenses Management" },
                    { 14, "subtitle", "Text", "Track and manage business expenses." },

                    // Employees
                    { 15, "title", "Text", "Employees" },
                    { 15, "subtitle", "Text", "Manage employee information and monthly schedules." },
                    { 15, "add_button_label", "Text", "Add Employee" },
                    { 15, "empty_title", "Text", "No employees found" },
                    { 15, "empty_body", "Text", "Click 'Add Employee' to create your first record." },

                    // Payroll
                    { 16, "title", "Text", "Payroll Management" },
                    { 16, "subtitle", "Text", "Employee payroll and benefits management." },
                    { 16, "empty_title", "Text", "No employees found" },
                    { 16, "empty_body", "Text", "Adjust filters or register new employees to view the payroll." },

                    // DailyClosing (Index)
                    { 17, "title", "Text", "Sales Dashboard" },
                    { 17, "subtitle", "Text", "Daily closing summary for" },
                    { 17, "empty_title", "Text", "No daily closing records found for this period" },
                    { 17, "empty_body", "Text", "Adjust your filters or submit a daily close to see data here." },

                    // DailyClosingCreate
                    { 18, "title", "Text", "Daily Close" },
                    { 18, "subtitle", "Text", "Process daily transactions with real-time calculations" },
                    { 18, "help_title", "Text", "Quick Help" },
                    { 18, "help_body", "Text", "All calculations are automated. Samer expenses are deducted from the adjusted reading along with general expenses and employee advances to calculate final cash." },

                    // Menu
                    { 19, "title", "Text", "Manage Menu" },
                    { 19, "subtitle", "Text", "Organize your kitchen offerings and prices." },
                    { 19, "add_button_label", "Text", "Add Category" },
                    { 19, "empty_title", "Text", "No categories yet" },
                    { 19, "empty_body", "Text", "Start building your menu by adding your first category." }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM [backoffice].[pageattributes] WHERE [PageId] BETWEEN 3 AND 19;");
            migrationBuilder.Sql("DELETE FROM [backoffice].[pages] WHERE [Id] BETWEEN 3 AND 19;");
        }
    }
}
