using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MiskBeirut.Application.Managers;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Repositories;
using MiskBeirut.Infrastructure.DbContexts;
using MiskBeirut.Infrastructure.Repositories;
using MiskBeirut.Web.Support;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<MiskBeirutDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MiskBeirut")));

// AddIdentityCore + AddRoles (not the full AddIdentity<TUser,TRole>) is the minimal-but-complete
// pattern: real many-to-many role membership (a user can hold several roles), no extra external-login
// or two-factor scheme wiring we don't use. The stock UserClaimsPrincipalFactory<User, IdentityRole<int>>
// already emits one ClaimTypes.Role claim per role the user belongs to, so [Authorize(Roles = "...")]
// works without any custom claims factory.
builder.Services.AddIdentityCore<User>(options =>
    {
        options.Password.RequireDigit = false;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
    })
    .AddRoles<IdentityRole<int>>()
    .AddEntityFrameworkStores<MiskBeirutDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<IUserClaimsPrincipalFactory<User>, UserClaimsPrincipalFactory<User, IdentityRole<int>>>();

builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddCookie(IdentityConstants.ApplicationScheme);

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth/Login";
    options.LogoutPath = "/Auth/Logout";
    options.AccessDeniedPath = "/Auth/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
});

// Repositories (Core interfaces -> Infrastructure implementations)
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IInvestorRepository, InvestorRepository>();
builder.Services.AddScoped<IReceiverRepository, ReceiverRepository>();
builder.Services.AddScoped<IExpenseRepository, ExpenseRepository>();
builder.Services.AddScoped<INonCashPaymentRepository, NonCashPaymentRepository>();
builder.Services.AddScoped<IDailyClosingRepository, DailyClosingRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IWebsiteLeadRepository, WebsiteLeadRepository>();
builder.Services.AddScoped<ILanguageRepository, LanguageRepository>();
builder.Services.AddScoped<IPageRepository, PageRepository>();
builder.Services.AddScoped<IGoogleReviewRepository, GoogleReviewRepository>();

// Application managers
builder.Services.AddScoped<CustomerManager>();
builder.Services.AddScoped<EmployeeManager>();
builder.Services.AddScoped<PayrollManager>();
builder.Services.AddScoped<InvestorManager>();
builder.Services.AddScoped<DailyClosingManager>();
builder.Services.AddScoped<ExpenseManager>();
builder.Services.AddScoped<NonCashPaymentManager>();
builder.Services.AddScoped<ReceiverManager>();
builder.Services.AddScoped<AuditLogManager>();
builder.Services.AddScoped<PageContentManager>();
builder.Services.AddScoped<WebsiteLeadManager>();
builder.Services.AddScoped<GoogleReviewManager>();

var app = builder.Build();

await AdminSeeder.SeedAsync(app.Services);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Subdomain-to-area mapping: backoffice.* -> Admin, cms.* -> Cms, everything else -> Customer.
// The area is supplied as a route default (not a literal URL segment), so it drives both inbound
// dispatch and asp-area link generation without ever putting /Admin or /Cms in a URL - matching
// the "no path prefixes" rule for this multi-subdomain, single-project setup. Host-specific routes
// must be registered before the catch-all "default" route so they win for their hosts.
//
// RequireHost's "*." wildcard only matches as a LEADING subdomain segment (e.g. "*.example.com");
// a trailing wildcard like "backoffice.*" is not a supported pattern and is compared literally,
// so it would never match a real Host header. List the known hosts explicitly instead
// (no port in the pattern means any port matches, so local dev ports work too).
app.MapControllerRoute(
    name: "admin",
    pattern: "{controller=Home}/{action=Index}/{id?}",
    defaults: new { area = "Admin" })
    .RequireHost("backoffice.localhost", "backoffice.miskbeirut.com");

app.MapControllerRoute(
    name: "cms",
    pattern: "{controller=Home}/{action=Index}/{id?}",
    defaults: new { area = "Cms" })
    .RequireHost("cms.localhost", "cms.miskbeirut.com");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}",
    defaults: new { area = "Customer" });

app.Run();
