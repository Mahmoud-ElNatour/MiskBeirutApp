using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MiskBeirut.Application.Managers;
using MiskBeirut.Application.Services;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Repositories;
using MiskBeirut.Infrastructure.DbContexts;
using MiskBeirut.Infrastructure.Repositories;
using MiskBeirut.Infrastructure.Services;
using MiskBeirut.Web.Authorization;
using MiskBeirut.Web.Support;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation();

// Needed by AuditLogManager to capture the caller's IP address on every logged action.
builder.Services.AddHttpContextAccessor();

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

// Dynamic, database-managed page/section privileges (Admin area "Role Manager"). Backs
// [RequirePrivilege] the same way [Authorize(Roles = "...")] is backed by role claims, except the
// role -> privilege mapping is looked up per request instead of baked into the sign-in cookie, so
// changes to a role's privileges take effect immediately without requiring users to re-log-in.
builder.Services.AddAuthorization();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PrivilegePolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PrivilegeAuthorizationHandler>();

// Repositories (Core interfaces -> Infrastructure implementations)
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IInvestorRepository, InvestorRepository>();
builder.Services.AddScoped<IReceiverRepository, ReceiverRepository>();
builder.Services.AddScoped<IExpenseRepository, ExpenseRepository>();
builder.Services.AddScoped<INonCashPaymentRepository, NonCashPaymentRepository>();
builder.Services.AddScoped<IDailyClosingRepository, DailyClosingRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IWebsiteLeadRepository, WebsiteLeadRepository>();
builder.Services.AddScoped<ILanguageRepository, LanguageRepository>();
builder.Services.AddScoped<IPageRepository, PageRepository>();
builder.Services.AddScoped<IBackofficePageRepository, BackofficePageRepository>();
builder.Services.AddScoped<IPrivilegeRepository, PrivilegeRepository>();
builder.Services.AddScoped<IGoogleReviewRepository, GoogleReviewRepository>();
builder.Services.AddScoped<IVacancyRepository, VacancyRepository>();
builder.Services.AddScoped<IJobApplicationRepository, JobApplicationRepository>();
builder.Services.AddScoped<IInquiryReasonRepository, InquiryReasonRepository>();
builder.Services.AddScoped<IContactInquiryRepository, ContactInquiryRepository>();
builder.Services.AddScoped<IContactInquiryWhatsAppMessageRepository, ContactInquiryWhatsAppMessageRepository>();

// Virus scanning — shared by CV submissions (Careers) and Cms image/menu-PDF uploads. Windows
// Defender is tried first; if MpCmdRun.exe isn't installed on this host, ClamAV (clamscan.exe) is
// tried as a fallback, so uploads aren't blanket-rejected just because one specific engine is
// missing. A scan only reports ScanUnavailable (which every caller fails closed on) if BOTH are
// unavailable. Stateless (just wraps external exe paths), so it's registered once as a singleton.
builder.Services.AddSingleton<IVirusScanner>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();

    var mpCmdRunPath = config["VirusScanning:MpCmdRunPath"];
    if (string.IsNullOrWhiteSpace(mpCmdRunPath))
        mpCmdRunPath = WindowsDefenderVirusScanner.FindDefaultMpCmdRunPath();
    var defenderScanner = new WindowsDefenderVirusScanner(mpCmdRunPath, sp.GetRequiredService<ILogger<WindowsDefenderVirusScanner>>());

    var clamAvPath = config["VirusScanning:ClamAvPath"];
    if (string.IsNullOrWhiteSpace(clamAvPath))
        clamAvPath = ClamAvVirusScanner.FindDefaultClamAvPath();
    var clamAvScanner = new ClamAvVirusScanner(clamAvPath, sp.GetRequiredService<ILogger<ClamAvVirusScanner>>());

    return new FallbackVirusScanner([defenderScanner, clamAvScanner], sp.GetRequiredService<ILogger<FallbackVirusScanner>>());
});

// First-run ClamAV install: if it's not already present when the app starts, download the current
// release's Windows MSI and install it silently, so the fallback above actually has something to
// fall back to. Set VirusScanning:AutoInstallClamAv=false to disable (see ClamAvBootstrapService
// for why this needs the app pool identity to have installer permissions, and what happens if it
// doesn't). Fire-and-forget in the background — never blocks the app from starting.
builder.Services.AddHttpClient(nameof(ClamAvBootstrapService), c => c.Timeout = TimeSpan.FromMinutes(15));
builder.Services.AddHostedService(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var enabled = !string.Equals(config["VirusScanning:AutoInstallClamAv"], "false", StringComparison.OrdinalIgnoreCase);

    return new ClamAvBootstrapService(
        enabled,
        sp.GetRequiredService<IHttpClientFactory>(),
        sp.GetRequiredService<IHostApplicationLifetime>(),
        sp.GetRequiredService<ILogger<ClamAvBootstrapService>>());
});

// CV private storage (Careers page applications) — scanning itself is IVirusScanner above.
// Deliberately NOT under wwwroot: CVs carry applicants' personal info (name, phone, address),
// unlike public marketing images. Paths default to App_Data under the content root
// (not the bin/ output dir) unless overridden in config.
builder.Services.AddScoped<ICvSubmissionService>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var scanner = sp.GetRequiredService<IVirusScanner>();

    var tempDir = config["FileStorage:CvScanTempPath"];
    if (string.IsNullOrWhiteSpace(tempDir))
        tempDir = Path.Combine(env.ContentRootPath, "App_Data", "scan-temp");

    var storageDir = config["FileStorage:CvUploadsPath"];
    if (string.IsNullOrWhiteSpace(storageDir))
        storageDir = Path.Combine(env.ContentRootPath, "App_Data", "careers", "cv");

    return new WindowsDefenderCvSubmissionService(scanner, tempDir, storageDir);
});

// Mailgun (HR notification emails). Requires Mailgun:ApiKey / Mailgun:Domain / Mailgun:FromAddress in config.
builder.Services.AddHttpClient(nameof(MailgunEmailSender));
builder.Services.AddScoped<IEmailSender>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var domain = config["Mailgun:Domain"] ?? throw new InvalidOperationException("Mailgun:Domain is not configured.");
    var apiKey = config["Mailgun:ApiKey"] ?? throw new InvalidOperationException("Mailgun:ApiKey is not configured.");
    var fromAddress = config["Mailgun:FromAddress"] ?? $"no-reply@{domain}";
    return new MailgunEmailSender(httpClientFactory.CreateClient(nameof(MailgunEmailSender)), domain, apiKey, fromAddress);
});

// Meta WhatsApp Business Platform (Cloud API) — CMS "Send WhatsApp" button on Contact Inquiries.
// Requires WhatsApp:PhoneNumberId / WhatsApp:AccessToken / WhatsApp:TemplateName in config once the
// Meta Business app and template are approved; until then, sends fail with a friendly explanation
// instead of the app refusing to start (see NotConfiguredWhatsAppSender).
builder.Services.AddHttpClient(nameof(MetaWhatsAppSender));
builder.Services.AddScoped<IWhatsAppSender>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var phoneNumberId = config["WhatsApp:PhoneNumberId"];
    var accessToken = config["WhatsApp:AccessToken"];
    if (string.IsNullOrWhiteSpace(phoneNumberId) || string.IsNullOrWhiteSpace(accessToken))
        return new NotConfiguredWhatsAppSender();

    var apiVersion = config["WhatsApp:ApiVersion"] is { Length: > 0 } v ? v : "v21.0";
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    return new MetaWhatsAppSender(httpClientFactory.CreateClient(nameof(MetaWhatsAppSender)), phoneNumberId, accessToken, apiVersion);
});

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
builder.Services.AddScoped<BackofficePageContentManager>();
builder.Services.AddScoped<PrivilegeManager>();
builder.Services.AddScoped<WebsiteLeadManager>();
builder.Services.AddScoped<GoogleReviewManager>();
builder.Services.AddScoped<VacancyManager>();
builder.Services.AddScoped<JobApplicationManager>();
builder.Services.AddScoped<InquiryReasonManager>();
builder.Services.AddScoped(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var templateName = config["WhatsApp:TemplateName"] ?? "";
    var templateLanguage = config["WhatsApp:TemplateLanguage"] is { Length: > 0 } lang ? lang : "en";
    var defaultCountryCode = config["WhatsApp:DefaultCountryCode"] is { Length: > 0 } cc ? cc : "961";
    return new ContactInquiryManager(
        sp.GetRequiredService<IContactInquiryRepository>(),
        sp.GetRequiredService<IInquiryReasonRepository>(),
        sp.GetRequiredService<IEmailSender>(),
        sp.GetRequiredService<PageContentManager>(),
        sp.GetRequiredService<IWhatsAppSender>(),
        sp.GetRequiredService<IContactInquiryWhatsAppMessageRepository>(),
        templateName,
        templateLanguage,
        defaultCountryCode,
        sp.GetRequiredService<ILogger<ContactInquiryManager>>());
});

var app = builder.Build();

await AdminSeeder.SeedAsync(app.Services);
await PrivilegeSeeder.SeedAsync(app.Services);

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
