using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
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

// Generated links are lower-cased, so the public site links to "/en/about" and never to
// "/en/About". Routing matches either spelling, which is exactly the problem: two spellings of one
// page is duplicate content to a crawler. PublicUrlMiddleware redirects the mixed-case form to this
// one, and the canonical tag states it, so all three agree on a single address per page.
builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;

    // Names the "lang" constraint the public route pattern below uses, so only the languages the
    // site publishes can open a page (see LanguageRouteConstraint).
    options.ConstraintMap["lang"] = typeof(LanguageRouteConstraint);
});

// Razor HTML-encodes every interpolated value, and the default encoder's allow-list is Basic Latin
// only — so Arabic copy leaves the server as numeric character references ("&#x627;&#x62E;..."),
// not as Arabic. A browser decodes those transparently in HTML text, which is why most of the site
// looked fine; but any value that reaches JavaScript as a string literal and is then written with
// textContent (e.g. the Contact form resetting its "Reason for Contact" label after a successful
// submit) has nothing to decode it and renders the raw entities to the visitor. Widening the
// allow-list emits real UTF-8 for every script the site uses.
builder.Services.AddWebEncoders(options =>
    options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

// Needed by AuditLogManager to capture the caller's IP address on every logged action.
builder.Services.AddHttpContextAccessor();

// Canonical/hreflang/Open Graph URLs and the sitemap, all built off Site:CanonicalHost rather than
// off whichever hostname the visitor happened to arrive on.
builder.Services.AddSingleton<SiteUrls>();

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

// CV private storage (Careers page applications). Deliberately NOT under wwwroot: CVs carry
// applicants' personal info (name, phone, address), unlike public marketing images. Uploads are
// verified by FileTypeValidator (extension + declared content type + actual byte signature) before
// they reach this service. Paths default to App_Data under the content root (not the bin/ output
// dir) unless overridden in config.
builder.Services.AddScoped<ICvSubmissionService>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var env = sp.GetRequiredService<IWebHostEnvironment>();

    var tempDir = config["FileStorage:CvUploadTempPath"];
    if (string.IsNullOrWhiteSpace(tempDir))
        tempDir = Path.Combine(env.ContentRootPath, "App_Data", "upload-temp");

    var storageDir = config["FileStorage:CvUploadsPath"];
    if (string.IsNullOrWhiteSpace(storageDir))
        storageDir = Path.Combine(env.ContentRootPath, "App_Data", "careers", "cv");

    return new FileSystemCvSubmissionService(tempDir, storageDir);
});

// Mailgun (HR notification emails, and the Cms "Email Sender" on inquiries/applications).
// Every setting is checked for BLANK, not just null: appsettings.json ships the keys with empty
// string values, so a `?? throw` passed a blank domain straight through and every send posted to
// "https://api.mailgun.net/v3//messages" — a 404 that read like a broken endpoint rather than
// missing configuration. An unconfigured install now gets a sender that says exactly what to set.
builder.Services.AddHttpClient(nameof(MailgunEmailSender));
builder.Services.AddScoped<IEmailSender>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();

    var domain = config["Mailgun:Domain"];
    var apiKey = config["Mailgun:ApiKey"];
    if (string.IsNullOrWhiteSpace(domain) || string.IsNullOrWhiteSpace(apiKey))
    {
        sp.GetRequiredService<ILogger<Program>>().LogError(
            "Mailgun is not configured (Mailgun:Domain and Mailgun:ApiKey must both be set in appsettings.json). Email sending is disabled until they are.");
        return new NotConfiguredEmailSender();
    }

    var fromAddress = config["Mailgun:FromAddress"];
    if (string.IsNullOrWhiteSpace(fromAddress))
        fromAddress = $"no-reply@{domain}";

    // EU-region Mailgun domains live on api.eu.mailgun.net; posting them to the US host is the
    // other way this returns 404. Left at the US default unless configured.
    var apiBaseUrl = config["Mailgun:ApiBaseUrl"];
    if (string.IsNullOrWhiteSpace(apiBaseUrl))
        apiBaseUrl = "https://api.mailgun.net";

    return new MailgunEmailSender(httpClientFactory.CreateClient(nameof(MailgunEmailSender)), domain, apiKey, fromAddress, apiBaseUrl);
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

// Instagram gallery on the Home page. Registered as a singleton because it caches the account's
// recent posts in memory: the page must not make an outbound call to Meta on every visit, and the
// cache is only useful if it outlives the request that filled it.
//
// Unconfigured installs get a feed that returns nothing, which the Home page reads as "fall back to
// the photographs an editor uploaded". That is the correct behaviour both before the Meta app
// exists and afterwards if the token lapses, so there is no separate failure path to write.
builder.Services.AddHttpClient(nameof(InstagramGraphFeed));
builder.Services.AddSingleton<IInstagramFeed>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var userId = config["Instagram:UserId"];
    var accessToken = config["Instagram:AccessToken"];
    if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(accessToken))
    {
        sp.GetRequiredService<ILogger<Program>>().LogInformation(
            "Instagram is not configured (Instagram:UserId and Instagram:AccessToken). The Home gallery will show the images uploaded through the Cms.");
        return new NotConfiguredInstagramFeed();
    }

    // graph.instagram.com for a token from Instagram Login, graph.facebook.com for one obtained
    // through a linked Facebook Page. Both answer the same /{ig-user-id}/media request, so which
    // host to use is configuration rather than a code change.
    var apiBaseUrl = config["Instagram:ApiBaseUrl"] is { Length: > 0 } baseUrl ? baseUrl : "https://graph.instagram.com";
    var apiVersion = config["Instagram:ApiVersion"] is { Length: > 0 } version ? version : "v21.0";

    // Fetched once per cache window and sliced per request, so this is the ceiling on how many
    // tiles the gallery can ever show, not how many it shows today.
    var fetchCount = int.TryParse(config["Instagram:FetchCount"], out var count) && count > 0 ? Math.Min(count, 25) : 12;
    var cacheMinutes = int.TryParse(config["Instagram:CacheMinutes"], out var minutes) && minutes > 0 ? minutes : 60;

    return new InstagramGraphFeed(
        sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(InstagramGraphFeed)),
        userId,
        accessToken,
        apiBaseUrl,
        apiVersion,
        fetchCount,
        TimeSpan.FromMinutes(cacheMinutes),
        sp.GetRequiredService<ILogger<InstagramGraphFeed>>());
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

// TLS is terminated in front of this app -- by Cloudflare today, by IIS on the deployment box --
// and what reaches Kestrel is a plain HTTP request on a local port. Without these headers the app
// believes every request arrived unencrypted, so UseHttpsRedirection has nothing to redirect and
// PublicUrlMiddleware cannot tell a genuinely insecure request from a proxied secure one; the
// result was that http://miskbeirut.com served the entire site over plain HTTP.
//
// The default trusted-proxy list (loopback only) is deliberately left alone. Both front doors are
// local to the app -- cloudflared connects to localhost, and IIS hands off over loopback too -- so
// loopback is exactly the set that may assert these headers. Widening it would let any client that
// could reach Kestrel directly claim its own X-Forwarded-Proto and defeat the redirect below.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// One URL per page: canonical host, lower-case spelling, no trailing slash, and a language prefix
// on everything public. Sits between static files (which it must not touch) and routing (which
// should only ever see a normalized path).
app.UseMiddleware<PublicUrlMiddleware>();

// A missing public page has to answer 404 AND look like the site. Without this, ASP.NET returns a
// bare status code with an empty body: correct to a crawler, a blank white page to a visitor.
// ReExecute keeps the 404 status while rendering the real view, which is what a redirect to an
// error page would throw away (that answers 302 then 200, and the broken URL never gets reported).
//
// The re-execute path has to name a language because every Customer route now carries one; which
// language the page actually renders in is decided inside the action, from the visitor's cookie.
//
// Public host only: the back-office subdomains are behind a login and have their own chrome, and a
// marketing 404 with a "Browse our menu" button is not what a missing admin screen should show.
app.UseWhen(context => PublicUrlMiddleware.IsPublicHost(context.Request.Host),
    branch => branch.UseStatusCodePagesWithReExecute($"/{SiteLanguages.Default}/home/pagenotfound"));

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

// Public pages live under a language segment: /en/about and /ar/about are distinct URLs that a
// crawler can index, canonicalize and cross-reference with hreflang.
//
// This is the ONLY route for the Customer area, deliberately. An unprefixed "{controller}/{action}"
// route alongside it also satisfies every asp-controller/asp-action link on the site, and link
// generation picked it — so the Arabic pages rendered a nav full of "/about" links that dropped the
// visitor back into English on the next click, and gave the crawler an unprefixed duplicate of
// every page to find. With one route, a generated link cannot lose its language.
//
// {lang} deliberately has NO default. Giving it one lets URL generation treat the segment as
// satisfied and drop it, so every asp-controller link on the site came out as "/about" again —
// prefix-free, and pointing at a URL that only redirects. Without a default the segment must be
// filled from the current request's route values, which is what keeps an Arabic page's links
// Arabic. Where nothing is ambient (the Cms's visual preview renders these public views from a
// /Pages/Edit/{id} URL) the previewing controller supplies the value itself.
app.MapAreaControllerRoute(
    name: "localized",
    areaName: "Customer",
    pattern: "{lang:lang}/{controller=Home}/{action=Index}/{id?}");

app.Run();
