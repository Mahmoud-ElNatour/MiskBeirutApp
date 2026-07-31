using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using MiskBeirut.Core.Entities;
using MiskBeirut.Infrastructure.DbContexts;

const string globalPageName = "Global";
const string mapsUrlAttribute = "google_maps_reviews_url";
const string storageStatePath = "auth-storage-state.json";

// One-time interactive setup: 'dotnet run -- login' opens a real, visible browser window
// so you can sign into the Google account the scraper should use. You do the actual sign-in
// yourself (including any 2FA/CAPTCHA) - this only captures the resulting session afterward,
// never your credentials. Re-run this whenever the saved session eventually expires.
if (args.Length > 0 && string.Equals(args[0], "login", StringComparison.OrdinalIgnoreCase))
{
    using var loginPlaywright = await Playwright.CreateAsync();
    await using var loginBrowser = await loginPlaywright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false });
    var loginPage = await loginBrowser.NewPageAsync(new BrowserNewPageOptions { Locale = "en-US" });
    await loginPage.GotoAsync("https://accounts.google.com/", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

    Console.WriteLine("A browser window has opened. Sign in there with the Google account the scraper should use.");
    Console.WriteLine("Once you're fully signed in (past any 'Stay signed in?' prompt), come back here and press Enter.");
    Console.ReadLine();

    await loginPage.Context.StorageStateAsync(new BrowserContextStorageStateOptions { Path = storageStatePath });
    await loginBrowser.CloseAsync();

    Console.WriteLine($"Session saved to {storageStatePath}. Run the scraper normally (no arguments) from now on.");
    return 0;
}

if (!File.Exists(storageStatePath))
{
    Console.WriteLine($"[{DateTime.UtcNow:u}] No saved login session found ('{storageStatePath}'). Run 'dotnet run -- login' once, sign in, then re-run.");
    return 1;
}

var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
var config = JsonDocument.Parse(File.ReadAllText(configPath));
var connectionString = config.RootElement.GetProperty("ConnectionStrings").GetProperty("MiskBeirut").GetString()
    ?? throw new InvalidOperationException("ConnectionStrings:MiskBeirut is not configured in appsettings.json.");

var dbOptions = new DbContextOptionsBuilder<MiskBeirutDbContext>()
    .UseSqlServer(connectionString)
    .Options;

await using var db = new MiskBeirutDbContext(dbOptions);

var globalPage = await db.Pages
    .Include(p => p.Attributes)
    .FirstOrDefaultAsync(p => p.PageName == globalPageName);

var mapsUrl = globalPage?.Attributes.FirstOrDefault(a => a.AttributeName == mapsUrlAttribute)?.Value;

if (string.IsNullOrWhiteSpace(mapsUrl))
{
    Console.WriteLine($"[{DateTime.UtcNow:u}] '{mapsUrlAttribute}' is not set on the '{globalPageName}' page (customer.page_attributes). Set it to the restaurant's Google Maps listing URL and re-run.");
    return 1;
}

// Google picks the UI language/layout from the request's geolocation, not the browser's
// locale setting - force English via the hl= query param so the page/selectors are stable
// regardless of which region the scraper happens to run from.
var separator = mapsUrl.Contains('?') ? "&" : "?";
var navigateUrl = $"{mapsUrl}{separator}hl=en";

Console.WriteLine($"[{DateTime.UtcNow:u}] Scraping reviews from: {navigateUrl}");

using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
var page = await browser.NewPageAsync(new BrowserNewPageOptions
{
    Locale = "en-US",
    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36",
    ExtraHTTPHeaders = new Dictionary<string, string> { ["Accept-Language"] = "en-US,en;q=0.9" },
    StorageStatePath = storageStatePath
});

ReviewJson[] scraped;

try
{
    await page.GotoAsync(navigateUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60000 });

    // Dismiss the EU cookie-consent interstitial, if Google shows one for this request.
    try
    {
        await page.GetByRole(AriaRole.Button, new() { Name = "Accept all" })
            .ClickAsync(new LocatorClickOptions { Timeout = 4000 });
        await page.WaitForTimeoutAsync(1000);
    }
    catch (TimeoutException)
    {
        // No consent dialog - continue.
    }

    // Switch to the Reviews tab if the listing opened on the Overview tab.
    try
    {
        await page.GetByRole(AriaRole.Tab, new() { Name = "Reviews", Exact = false }).First
            .ClickAsync(new LocatorClickOptions { Timeout = 4000 });
        await page.WaitForTimeoutAsync(1500);
    }
    catch (TimeoutException)
    {
        // Already showing reviews, or no separate tab - continue.
    }

    await page.Locator("div.jftiEf").First.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });

    // The review feed lazy-loads as its pane is scrolled. There's no reliable top-level
    // selector for the scrollable pane itself, so find it by walking up from a review
    // card to its nearest scrollable ancestor, then scroll that a handful of times so
    // enough cards are in the DOM to reliably find 3 five-star reviews.
    var feedHandle = await page.EvaluateHandleAsync(@"() => {
        const card = document.querySelector('div.jftiEf');
        let el = card;
        while (el) {
            const style = getComputedStyle(el);
            if ((style.overflowY === 'auto' || style.overflowY === 'scroll') && el.scrollHeight > el.clientHeight) {
                return el;
            }
            el = el.parentElement;
        }
        return null;
    }");

    for (var i = 0; i < 10; i++)
    {
        await page.EvaluateAsync("el => { if (el) el.scrollBy(0, 1200); }", feedHandle);
        await page.WaitForTimeoutAsync(600);
    }

    // NOTE: these selectors target Google Maps' current (obfuscated, versioned) review-card
    // markup. Google rotates these class names periodically - if a run stops finding reviews,
    // open a live Maps listing, inspect a review card, and update the selectors below.
    scraped = await page.EvaluateAsync<ReviewJson[]>(@"() => {
        const cards = Array.from(document.querySelectorAll('div.jftiEf'));
        return cards.map(card => {
            const authorEl = card.querySelector('.d4r55');
            const ratingEl = card.querySelector('span[aria-label*=""star""]');
            const textEl = card.querySelector('.wiI7pd');
            const timeEl = card.querySelector('.rsqaWe');
            const photoEl = card.querySelector('img.NBa7we');
            const ratingMatch = ratingEl ? ratingEl.getAttribute('aria-label').match(/(\d+(\.\d+)?)/) : null;

            return {
                author: authorEl ? authorEl.textContent.trim() : null,
                rating: ratingMatch ? Math.round(parseFloat(ratingMatch[1])) : 0,
                text: textEl ? textEl.textContent.trim() : null,
                relativeTime: timeEl ? timeEl.textContent.trim() : null,
                photoUrl: photoEl ? photoEl.getAttribute('src') : null
            };
        });
    }");
}
finally
{
    await browser.CloseAsync();
}

var fiveStar = scraped
    .Where(r => r.Rating == 5 && !string.IsNullOrWhiteSpace(r.Author) && !string.IsNullOrWhiteSpace(r.Text))
    .Take(3)
    .ToList();

if (fiveStar.Count == 0)
{
    Console.WriteLine($"[{DateTime.UtcNow:u}] No 5-star reviews with a written description found (scanned {scraped.Length} review card(s)). Database left unchanged.");
    return 1;
}

var existing = await db.GoogleReviews.ToListAsync();
db.GoogleReviews.RemoveRange(existing);

for (var i = 0; i < fiveStar.Count; i++)
{
    var r = fiveStar[i];
    db.GoogleReviews.Add(new GoogleReview
    {
        AuthorName = r.Author!,
        Rating = r.Rating,
        ReviewText = r.Text,
        RelativeTime = r.RelativeTime,
        ProfilePhotoUrl = r.PhotoUrl,
        DisplayOrder = i + 1
    });
}

await db.SaveChangesAsync();

Console.WriteLine($"[{DateTime.UtcNow:u}] Saved {fiveStar.Count} five-star review(s) to customer.google_reviews.");
return 0;

record ReviewJson(string? Author, int Rating, string? Text, string? RelativeTime, string? PhotoUrl);
