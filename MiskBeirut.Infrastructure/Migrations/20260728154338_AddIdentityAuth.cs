using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiskBeirut.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "backoffice");

            migrationBuilder.EnsureSchema(
                name: "customer");

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                schema: "backoffice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Username = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "customers",
                schema: "backoffice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "customers",
                schema: "customer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DiscountRedeemed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "daily_closing",
                schema: "backoffice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    MainReading = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AdjustedReading = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ActualCash = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_closing", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "investors",
                schema: "backoffice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_investors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "languages",
                schema: "customer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_languages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "pages",
                schema: "customer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PageName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MetaTitle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    MetaDesc = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MetaKeyword = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "receivers",
                schema: "backoffice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_receivers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "backoffice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "customer_ledger",
                schema: "customer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    DailyClosingId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_ledger", x => x.Id);
                    table.CheckConstraint("CK_customer_ledger_sign", "([Type] = 'Credit' AND [Amount] < 0) OR ([Type] = 'Cashback' AND [Amount] > 0)");
                    table.CheckConstraint("CK_customer_ledger_type", "[Type] IN ('Credit', 'Cashback')");
                    table.ForeignKey(
                        name: "FK_customer_ledger_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "backoffice",
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_customer_ledger_daily_closing_DailyClosingId",
                        column: x => x.DailyClosingId,
                        principalSchema: "backoffice",
                        principalTable: "daily_closing",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "non_cash_payments",
                schema: "backoffice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DailyClosingId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_non_cash_payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_non_cash_payments_daily_closing_DailyClosingId",
                        column: x => x.DailyClosingId,
                        principalSchema: "backoffice",
                        principalTable: "daily_closing",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "page_attributes",
                schema: "customer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PageId = table.Column<int>(type: "int", nullable: false),
                    AttributeName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AttributeType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Text"),
                    LangId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_page_attributes", x => x.Id);
                    table.CheckConstraint("CK_page_attributes_type", "[AttributeType] IN ('Text', 'RichText', 'Image', 'Link', 'Video', 'Number', 'Date', 'Boolean')");
                    table.ForeignKey(
                        name: "FK_page_attributes_languages_LangId",
                        column: x => x.LangId,
                        principalSchema: "customer",
                        principalTable: "languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_page_attributes_pages_PageId",
                        column: x => x.PageId,
                        principalSchema: "customer",
                        principalTable: "pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "expenses",
                schema: "backoffice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DailyClosingId = table.Column<int>(type: "int", nullable: false),
                    ReceiverId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_expenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_expenses_daily_closing_DailyClosingId",
                        column: x => x.DailyClosingId,
                        principalSchema: "backoffice",
                        principalTable: "daily_closing",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_expenses_receivers_ReceiverId",
                        column: x => x.ReceiverId,
                        principalSchema: "backoffice",
                        principalTable: "receivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "investor_transactions",
                schema: "backoffice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TransactionType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DailyClosingId = table.Column<int>(type: "int", nullable: false),
                    InvestorId = table.Column<int>(type: "int", nullable: false),
                    ReceiverId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_investor_transactions", x => x.Id);
                    table.CheckConstraint("CK_investor_txn_receiver", "[TransactionType] <> 'Expense' OR [ReceiverId] IS NOT NULL");
                    table.CheckConstraint("CK_investor_txn_type", "[TransactionType] IN ('Withdrawal', 'Expense')");
                    table.ForeignKey(
                        name: "FK_investor_transactions_daily_closing_DailyClosingId",
                        column: x => x.DailyClosingId,
                        principalSchema: "backoffice",
                        principalTable: "daily_closing",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_investor_transactions_investors_InvestorId",
                        column: x => x.InvestorId,
                        principalSchema: "backoffice",
                        principalTable: "investors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_investor_transactions_receivers_ReceiverId",
                        column: x => x.ReceiverId,
                        principalSchema: "backoffice",
                        principalTable: "receivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "employees",
                schema: "backoffice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Position = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BaseSalary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_employees_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "backoffice",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_claims",
                schema: "backoffice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_claims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_claims_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "backoffice",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_logins",
                schema: "backoffice",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_logins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_user_logins_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "backoffice",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_tokens",
                schema: "backoffice",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_tokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_user_tokens_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "backoffice",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employee_ledger",
                schema: "backoffice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    DailyClosingId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_ledger", x => x.Id);
                    table.CheckConstraint("CK_employee_ledger_sign", "[Amount] < 0");
                    table.CheckConstraint("CK_employee_ledger_type", "[Type] IN ('Advance', 'Deduct')");
                    table.ForeignKey(
                        name: "FK_employee_ledger_daily_closing_DailyClosingId",
                        column: x => x.DailyClosingId,
                        principalSchema: "backoffice",
                        principalTable: "daily_closing",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employee_ledger_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "backoffice",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "employee_working",
                schema: "backoffice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    WorkingDays = table.Column<int>(type: "int", nullable: true),
                    ActualWorkingDays = table.Column<int>(type: "int", nullable: true),
                    DeductionsTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    AdvanceTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ActualSalary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    StartedAt = table.Column<DateOnly>(type: "date", nullable: true),
                    EndedAt = table.Column<DateOnly>(type: "date", nullable: true),
                    IsWorking = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_working", x => x.Id);
                    table.ForeignKey(
                        name: "FK_employee_working_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "backoffice",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_customer_ledger_CustomerId",
                schema: "customer",
                table: "customer_ledger",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_customer_ledger_DailyClosingId",
                schema: "customer",
                table: "customer_ledger",
                column: "DailyClosingId");

            migrationBuilder.CreateIndex(
                name: "IX_daily_closing_Date",
                schema: "backoffice",
                table: "daily_closing",
                column: "Date",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employee_ledger_DailyClosingId",
                schema: "backoffice",
                table: "employee_ledger",
                column: "DailyClosingId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_ledger_EmployeeId",
                schema: "backoffice",
                table: "employee_ledger",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_working_EmployeeId",
                schema: "backoffice",
                table: "employee_working",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_employees_UserId",
                schema: "backoffice",
                table: "employees",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_expenses_DailyClosingId",
                schema: "backoffice",
                table: "expenses",
                column: "DailyClosingId");

            migrationBuilder.CreateIndex(
                name: "IX_expenses_ReceiverId",
                schema: "backoffice",
                table: "expenses",
                column: "ReceiverId");

            migrationBuilder.CreateIndex(
                name: "IX_investor_transactions_DailyClosingId",
                schema: "backoffice",
                table: "investor_transactions",
                column: "DailyClosingId");

            migrationBuilder.CreateIndex(
                name: "IX_investor_transactions_InvestorId",
                schema: "backoffice",
                table: "investor_transactions",
                column: "InvestorId");

            migrationBuilder.CreateIndex(
                name: "IX_investor_transactions_ReceiverId",
                schema: "backoffice",
                table: "investor_transactions",
                column: "ReceiverId");

            migrationBuilder.CreateIndex(
                name: "IX_languages_Code",
                schema: "customer",
                table: "languages",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_non_cash_payments_DailyClosingId",
                schema: "backoffice",
                table: "non_cash_payments",
                column: "DailyClosingId");

            migrationBuilder.CreateIndex(
                name: "IX_page_attributes_LangId",
                schema: "customer",
                table: "page_attributes",
                column: "LangId");

            migrationBuilder.CreateIndex(
                name: "UQ_page_attributes_PageAttrLang",
                schema: "customer",
                table: "page_attributes",
                columns: new[] { "PageId", "AttributeName", "LangId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pages_PageName",
                schema: "customer",
                table: "pages",
                column: "PageName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_claims_UserId",
                schema: "backoffice",
                table: "user_claims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_logins_UserId",
                schema: "backoffice",
                table: "user_logins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "backoffice",
                table: "users",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "backoffice",
                table: "users",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs",
                schema: "backoffice");

            migrationBuilder.DropTable(
                name: "customer_ledger",
                schema: "customer");

            migrationBuilder.DropTable(
                name: "customers",
                schema: "customer");

            migrationBuilder.DropTable(
                name: "employee_ledger",
                schema: "backoffice");

            migrationBuilder.DropTable(
                name: "employee_working",
                schema: "backoffice");

            migrationBuilder.DropTable(
                name: "expenses",
                schema: "backoffice");

            migrationBuilder.DropTable(
                name: "investor_transactions",
                schema: "backoffice");

            migrationBuilder.DropTable(
                name: "non_cash_payments",
                schema: "backoffice");

            migrationBuilder.DropTable(
                name: "page_attributes",
                schema: "customer");

            migrationBuilder.DropTable(
                name: "user_claims",
                schema: "backoffice");

            migrationBuilder.DropTable(
                name: "user_logins",
                schema: "backoffice");

            migrationBuilder.DropTable(
                name: "user_tokens",
                schema: "backoffice");

            migrationBuilder.DropTable(
                name: "customers",
                schema: "backoffice");

            migrationBuilder.DropTable(
                name: "employees",
                schema: "backoffice");

            migrationBuilder.DropTable(
                name: "investors",
                schema: "backoffice");

            migrationBuilder.DropTable(
                name: "receivers",
                schema: "backoffice");

            migrationBuilder.DropTable(
                name: "daily_closing",
                schema: "backoffice");

            migrationBuilder.DropTable(
                name: "languages",
                schema: "customer");

            migrationBuilder.DropTable(
                name: "pages",
                schema: "customer");

            migrationBuilder.DropTable(
                name: "users",
                schema: "backoffice");
        }
    }
}
