using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Enums;

namespace MiskBeirut.Infrastructure.DbContexts;

/// <summary>
/// Maps the domain entities to the existing MiskBeirutDb schema (backoffice.* and customer.*),
/// replicating the constraints of the SQL script the database was created from. Enum
/// properties are stored as strings so the existing NVARCHAR columns and CHECK
/// constraints stay untouched.
///
/// Derives from <see cref="IdentityDbContext{TUser,TRole,TKey}"/>: a user can hold more than one
/// role at once, so role membership needs the real many-to-many roles/user_roles tables rather
/// than a single column.
/// </summary>
public class MiskBeirutDbContext : IdentityDbContext<User, IdentityRole<int>, int>
{
    public MiskBeirutDbContext(DbContextOptions<MiskBeirutDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeWorking> EmployeeWorkings => Set<EmployeeWorking>();
    public DbSet<EmployeeLedger> EmployeeLedgers => Set<EmployeeLedger>();
    public DbSet<Investor> Investors => Set<Investor>();
    public DbSet<Receiver> Receivers => Set<Receiver>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<InvestorTransaction> InvestorTransactions => Set<InvestorTransaction>();
    public DbSet<NonCashPayment> NonCashPayments => Set<NonCashPayment>();
    public DbSet<DailyClosing> DailyClosings => Set<DailyClosing>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<WebsiteLead> WebsiteLeads => Set<WebsiteLead>();
    public DbSet<CustomerLedger> CustomerLedgers => Set<CustomerLedger>();
    public DbSet<Language> Languages => Set<Language>();
    public DbSet<Page> Pages => Set<Page>();
    public DbSet<PageAttribute> PageAttributes => Set<PageAttribute>();
    public DbSet<GoogleReview> GoogleReviews => Set<GoogleReview>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users", "backoffice");
            entity.Ignore(u => u.Username);
            entity.Property(u => u.UserName).HasColumnName("Username").HasMaxLength(100).IsRequired();
            entity.Property(u => u.Email).HasMaxLength(256);
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(u => u.AccessFailedCount).HasDefaultValue(0);
            entity.Property(u => u.EmailConfirmed).HasDefaultValue(false);
            entity.Property(u => u.LockoutEnabled).HasDefaultValue(true);
            entity.Property(u => u.NormalizedEmail).HasMaxLength(256);
            entity.Property(u => u.NormalizedUserName).HasMaxLength(256);
            entity.Property(u => u.PhoneNumber).HasMaxLength(50);
            entity.Property(u => u.PhoneNumberConfirmed).HasDefaultValue(false);
        });

        modelBuilder.Entity<IdentityRole<int>>(entity => entity.ToTable("roles", "backoffice"));
        modelBuilder.Entity<IdentityUserRole<int>>(entity => entity.ToTable("user_roles", "backoffice"));
        modelBuilder.Entity<IdentityRoleClaim<int>>(entity => entity.ToTable("role_claims", "backoffice"));
        modelBuilder.Entity<IdentityUserClaim<int>>(entity => entity.ToTable("user_claims", "backoffice"));
        modelBuilder.Entity<IdentityUserLogin<int>>(entity => entity.ToTable("user_logins", "backoffice"));
        modelBuilder.Entity<IdentityUserToken<int>>(entity => entity.ToTable("user_tokens", "backoffice"));

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("customers", "backoffice");
            entity.Property(c => c.Name).HasMaxLength(200).IsRequired();
            entity.Property(c => c.PhoneNumber).HasMaxLength(50).IsRequired();
            entity.Property(c => c.Balance).HasPrecision(18, 2).HasDefaultValue(0m);
            entity.Property(c => c.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.ToTable("employees", "backoffice");
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.PhoneNumber).HasMaxLength(50);
            entity.Property(e => e.Position).HasMaxLength(100);
            entity.Property(e => e.BaseSalary).HasPrecision(18, 2).HasDefaultValue(0m);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

            entity.HasOne(e => e.User)
                .WithMany(u => u.Employees)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmployeeWorking>(entity =>
        {
            entity.ToTable("employee_working", "backoffice");
            entity.Property(w => w.Status).HasMaxLength(50);
            entity.Property(w => w.DeductionsTotal).HasPrecision(18, 2);
            entity.Property(w => w.AdvanceTotal).HasPrecision(18, 2);
            entity.Property(w => w.ActualSalary).HasPrecision(18, 2);
            entity.Property(w => w.Total).HasPrecision(18, 2);
            entity.Property(w => w.IsWorking).HasDefaultValue(true);
            entity.Property(w => w.Note).HasMaxLength(500);

            entity.HasOne(w => w.Employee)
                .WithMany(e => e.WorkingRecords)
                .HasForeignKey(w => w.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmployeeLedger>(entity =>
        {
            entity.ToTable("employee_ledger", "backoffice", t =>
            {
                t.HasCheckConstraint("CK_employee_ledger_type", "[Type] IN ('Advance', 'Deduct')");
                t.HasCheckConstraint("CK_employee_ledger_sign", "[Amount] < 0");
            });
            entity.Property(l => l.Amount).HasPrecision(18, 2);
            entity.Property(l => l.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(l => l.Note).HasMaxLength(500);

            entity.HasOne(l => l.Employee)
                .WithMany(e => e.LedgerEntries)
                .HasForeignKey(l => l.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(l => l.DailyClosing)
                .WithMany(d => d.EmployeeLedgerEntries)
                .HasForeignKey(l => l.DailyClosingId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Investor>(entity =>
        {
            entity.ToTable("investors", "backoffice");
            entity.Property(i => i.Name).HasMaxLength(200).IsRequired();
            entity.Property(i => i.IsActive).HasDefaultValue(true);
            entity.Property(i => i.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<Receiver>(entity =>
        {
            entity.ToTable("receivers", "backoffice");
            entity.Property(r => r.Name).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<Expense>(entity =>
        {
            entity.ToTable("expenses", "backoffice");
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Note).HasMaxLength(500);

            entity.HasOne(e => e.DailyClosing)
                .WithMany(d => d.Expenses)
                .HasForeignKey(e => e.DailyClosingId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Receiver)
                .WithMany(r => r.Expenses)
                .HasForeignKey(e => e.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InvestorTransaction>(entity =>
        {
            entity.ToTable("investor_transactions", "backoffice", t =>
            {
                t.HasCheckConstraint("CK_investor_txn_type", "[TransactionType] IN ('Withdrawal', 'Expense')");
                t.HasCheckConstraint("CK_investor_txn_receiver", "[TransactionType] <> 'Expense' OR [ReceiverId] IS NOT NULL");
            });
            entity.Property(t => t.Amount).HasPrecision(18, 2);
            entity.Property(t => t.TransactionType).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(t => t.Note).HasMaxLength(500);

            entity.HasOne(t => t.DailyClosing)
                .WithMany(d => d.InvestorTransactions)
                .HasForeignKey(t => t.DailyClosingId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.Investor)
                .WithMany(i => i.Transactions)
                .HasForeignKey(t => t.InvestorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.Receiver)
                .WithMany(r => r.InvestorTransactions)
                .HasForeignKey(t => t.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<NonCashPayment>(entity =>
        {
            entity.ToTable("non_cash_payments", "backoffice");
            entity.Property(p => p.Amount).HasPrecision(18, 2);
            entity.Property(p => p.PaymentMethod).HasMaxLength(30).IsRequired();
            entity.Property(p => p.Note).HasMaxLength(500);

            entity.HasOne(p => p.DailyClosing)
                .WithMany(d => d.NonCashPayments)
                .HasForeignKey(p => p.DailyClosingId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DailyClosing>(entity =>
        {
            entity.ToTable("daily_closing", "backoffice");
            entity.HasIndex(d => d.Date).IsUnique();
            entity.Property(d => d.MainReading).HasPrecision(18, 2);
            entity.Property(d => d.AdjustedReading).HasPrecision(18, 2);
            entity.Property(d => d.ActualCash).HasPrecision(18, 2);
            entity.Property(d => d.Note).HasMaxLength(500);
            entity.Property(d => d.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs", "backoffice");
            entity.Property(a => a.EntityType).HasMaxLength(200);
            entity.Property(a => a.Action).HasMaxLength(100);
            entity.Property(a => a.EntityId).HasMaxLength(100);
            entity.Property(a => a.Username).HasMaxLength(200);
            entity.Property(a => a.Description).HasMaxLength(1000);
            entity.Property(a => a.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(a => a.IpAddress).HasMaxLength(50);
        });

        modelBuilder.Entity<WebsiteLead>(entity =>
        {
            entity.ToTable("customers", "customer");
            entity.Property(l => l.Name).HasMaxLength(200).IsRequired();
            entity.Property(l => l.PhoneNumber).HasMaxLength(50).IsRequired();
            entity.Property(l => l.Email).HasMaxLength(256).IsRequired();
            entity.Property(l => l.DiscountRedeemed).HasDefaultValue(false);
            entity.Property(l => l.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<CustomerLedger>(entity =>
        {
            entity.ToTable("customer_ledger", "customer", t =>
            {
                t.HasCheckConstraint("CK_customer_ledger_type", "[Type] IN ('Credit', 'Cashback')");
                t.HasCheckConstraint("CK_customer_ledger_sign",
                    "([Type] = 'Credit' AND [Amount] < 0) OR ([Type] = 'Cashback' AND [Amount] > 0)");
            });
            entity.Property(l => l.Amount).HasPrecision(18, 2);
            entity.Property(l => l.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(l => l.Note).HasMaxLength(500);

            entity.HasOne(l => l.Customer)
                .WithMany(c => c.LedgerEntries)
                .HasForeignKey(l => l.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(l => l.DailyClosing)
                .WithMany(d => d.CustomerLedgerEntries)
                .HasForeignKey(l => l.DailyClosingId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Language>(entity =>
        {
            entity.ToTable("languages", "customer");
            entity.HasIndex(l => l.Code).IsUnique();
            entity.Property(l => l.Code).HasMaxLength(10).IsRequired();
            entity.Property(l => l.Name).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<Page>(entity =>
        {
            entity.ToTable("pages", "customer");
            entity.HasIndex(p => p.PageName).IsUnique();
            entity.Property(p => p.PageName).HasMaxLength(200).IsRequired();
            entity.Property(p => p.MetaTitle).HasMaxLength(300);
            entity.Property(p => p.MetaDesc).HasMaxLength(500);
            entity.Property(p => p.MetaKeyword).HasMaxLength(500);
        });

        modelBuilder.Entity<PageAttribute>(entity =>
        {
            entity.ToTable("page_attributes", "customer", t =>
            {
                t.HasCheckConstraint("CK_page_attributes_type",
                    "[AttributeType] IN ('Text', 'RichText', 'Image', 'Link', 'Video', 'Number', 'Date', 'Boolean')");
            });
            entity.HasIndex(a => new { a.PageId, a.AttributeName, a.LangId })
                .IsUnique()
                .HasDatabaseName("UQ_page_attributes_PageAttrLang");
            entity.Property(a => a.AttributeName).HasMaxLength(200).IsRequired();
            entity.Property(a => a.AttributeType)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired()
                .HasDefaultValue(PageAttributeType.Text);

            entity.HasOne(a => a.Page)
                .WithMany(p => p.Attributes)
                .HasForeignKey(a => a.PageId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Language)
                .WithMany(l => l.PageAttributes)
                .HasForeignKey(a => a.LangId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GoogleReview>(entity =>
        {
            entity.ToTable("google_reviews", "customer", t =>
            {
                t.HasCheckConstraint("CK_google_reviews_rating", "[Rating] BETWEEN 1 AND 5");
            });
            entity.Property(r => r.AuthorName).HasMaxLength(200).IsRequired();
            entity.Property(r => r.ProfilePhotoUrl).HasMaxLength(500);
            entity.Property(r => r.RelativeTime).HasMaxLength(100);
            entity.Property(r => r.DisplayOrder).HasDefaultValue(0);
            entity.Property(r => r.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });
    }
}
