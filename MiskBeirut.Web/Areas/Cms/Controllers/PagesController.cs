using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Application.Dtos.Pages;
using MiskBeirut.Application.Managers;
using MiskBeirut.Application.Services;
using MiskBeirut.Core.Enums;
using MiskBeirut.Core.Repositories;
using MiskBeirut.Web.Areas.Cms.Models.Pages;
using MiskBeirut.Web.Support;

namespace MiskBeirut.Web.Areas.Cms.Controllers;

public class PagesController : CmsControllerBase
{
    private const string DefaultLangCode = "en";
    private const long MaxImageSizeBytes = 5 * 1024 * 1024; // 5 MB
    private const long MaxPdfSizeBytes = 20 * 1024 * 1024; // 20 MB

    private const string GlobalPageName = "Global";

    /// <summary>
    /// The "Menu" page gets its own edit screen (PDF preview + upload) instead of the generic
    /// attribute-row editor — see MenuPdfAttributeName below for where the value actually lives.
    /// </summary>
    private const string MenuPageName = "Menu";

    /// <summary>
    /// The current menu PDF's url, stored as a Link attribute on the shared "Global" page (so the
    /// public MenuController can read it via PageContent.Global(...) without a Menu page existing
    /// at all) rather than on the Menu page's own attributes.
    /// </summary>
    private const string MenuPdfAttributeName = "menu_pdf_url";

    /// <summary>
    /// Pages previewed by rendering the ACTUAL public Razor view (not a hand-built copy) — the view
    /// itself carries data-cms-field/data-cms-type attributes on its real content elements, which
    /// _CmsPreviewOverlay.cshtml uses to draw pencil buttons over them. Maps PageName to the public
    /// Customer-area controller that owns the view. Add an entry here once a page's real view has
    /// been annotated with data-cms-field attributes.
    /// </summary>
    private static readonly Dictionary<string, string> VisualPreviewPages = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Careers"] = "Careers",
        ["About"] = "About",
        ["Contact"] = "Contact",
        ["Events"] = "Events",
        ["Home"] = "Home",
        ["Menu"] = "Menu",
        ["Spaces"] = "Spaces"
    };

    private readonly PageContentManager _pages;
    private readonly ILanguageRepository _languages;
    private readonly VacancyManager _vacancies;
    private readonly IWebHostEnvironment _env;
    private readonly IVirusScanner _virusScanner;
    private readonly ILogger<PagesController> _logger;

    public PagesController(PageContentManager pages, ILanguageRepository languages, VacancyManager vacancies, IWebHostEnvironment env, IVirusScanner virusScanner, ILogger<PagesController> logger)
    {
        _pages = pages;
        _languages = languages;
        _vacancies = vacancies;
        _env = env;
        _virusScanner = virusScanner;
        _logger = logger;
    }

    /// <summary>Scratch space for a file mid-scan — same folder Careers CV submissions use (see Program.cs); GUID-named temp files never collide, so sharing it is fine.</summary>
    private string ScanTempDirectory => Path.Combine(_env.ContentRootPath, "App_Data", "scan-temp");

    public async Task<IActionResult> Index(string? q)
    {
        var pages = await _pages.GetAllPagesAsync();

        var filtered = string.IsNullOrWhiteSpace(q)
            ? pages
            : pages.Where(p =>
                p.PageName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (p.MetaTitle?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (p.MetaDesc?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                p.Attributes.Any(a =>
                    a.AttributeName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (a.Value?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)));

        ViewData["SearchQuery"] = q;
        return View(filtered.OrderBy(p => p.PageName).ToList());
    }

    /// <param name="view">
    /// "page" opens the visual preview instead of a page's specialised editor. Only Menu has one of
    /// those (the PDF upload screen), and it now also has real page copy around the embedded PDF —
    /// so this is how the Cms reaches that copy's pencil buttons without losing the uploader.
    /// </param>
    public async Task<IActionResult> Edit(int id, string? lang, string? view)
    {
        var page = await _pages.GetPageAsync(id);
        if (page is null)
            return NotFound();

        ViewData["CurrentPageId"] = id;

        var languages = await _languages.GetAllAsync();
        if (languages.Count == 0)
            return Problem("No languages are configured (customer.languages is empty).");

        var langCode = string.IsNullOrWhiteSpace(lang) ? DefaultLangCode : lang;
        var language = languages.FirstOrDefault(l => l.Code == langCode) ?? languages.First();
        var languageOptions = languages.OrderBy(l => l.Code)
            .Select(l => new LanguageOptionViewModel { Id = l.Id, Code = l.Code, Name = l.Name }).ToList();

        var wantsVisualPreview = string.Equals(view, "page", StringComparison.OrdinalIgnoreCase);

        if (string.Equals(page.PageName, MenuPageName, StringComparison.OrdinalIgnoreCase) && !wantsVisualPreview)
        {
            var global = await _pages.GetPageByNameAsync(GlobalPageName)
                ?? throw new InvalidOperationException("The Global page is missing — every install seeds it.");

            var pdfUrl = global.Attributes
                .FirstOrDefault(a => a.LangId == language.Id && a.AttributeName == MenuPdfAttributeName)
                ?.Value;

            return View("MenuEdit", new MenuEditViewModel
            {
                PageId = page.Id,
                GlobalPageId = global.Id,
                LangId = language.Id,
                LangCode = language.Code,
                Languages = languageOptions,
                PdfUrl = pdfUrl
            });
        }

        if (VisualPreviewPages.TryGetValue(page.PageName, out var publicController))
        {
            var global = await _pages.GetPageByNameAsync(GlobalPageName);
            var content = new PageContent(page, global, language.Id, language.Code);

            // Everything PublicContentController.LoadPageAsync would normally set, so the page's own
            // shared _PublicNav/_PublicFooter/_DiscountPopup partials render exactly as they do live.
            ViewData["Lang"] = language.Code;
            ViewData["Dir"] = content.IsRtl ? "rtl" : "ltr";
            ViewData["Title"] = page.MetaTitle ?? $"{page.PageName} | Misk Beirut";
            ViewData["MetaDescription"] = page.MetaDesc;
            ViewData["Content"] = content;

            // Cms preview chrome (see _PublicLayout.cshtml + _CmsPreviewOverlay.cshtml).
            ViewData["IsCmsPreview"] = true;
            ViewData["PageId"] = page.Id;
            ViewData["GlobalPageId"] = global?.Id;
            ViewData["PageName"] = page.PageName;
            ViewData["LangId"] = language.Id;
            ViewData["LangCode"] = language.Code;
            ViewData["Languages"] = languageOptions;

            if (string.Equals(page.PageName, "Careers", StringComparison.OrdinalIgnoreCase))
                ViewData["Vacancies"] = await _vacancies.GetActiveAsync(language.Code);

            // The real nav/footer's own Home/About/Menu/... links point at the live public site by
            // design (that's what "render the actual view" below means) — this map lets
            // _CmsPreviewBar.cshtml's click interceptor keep those clicks inside the Cms area instead.
            ViewData["PreviewNavMap"] = await BuildPreviewNavMapAsync(language.Code);

            // Match the ambient route values the real Customer page would have, so its asp-controller/
            // asp-action links (nav, footer, "Apply") and Layout/partial lookups resolve against the
            // Customer area instead of Cms — this only affects how THIS response renders, not routing
            // or authorization, which already ran against the real Cms route.
            RouteData.Values["area"] = "Customer";
            RouteData.Values["controller"] = publicController;
            RouteData.Values["action"] = "Index";

            return View($"~/Areas/Customer/Views/{publicController}/Index.cshtml", content);
        }

        var vm = new PageEditViewModel
        {
            PageId = page.Id,
            PageName = page.PageName,
            MetaTitle = page.MetaTitle,
            MetaDesc = page.MetaDesc,
            MetaKeyword = page.MetaKeyword,
            LangId = language.Id,
            LangCode = language.Code,
            Languages = languageOptions,
            Attributes = page.Attributes
                .Where(a => a.LangId == language.Id)
                .OrderBy(a => a.AttributeName)
                .Select(a => new PageAttributeRowViewModel
                {
                    AttributeName = a.AttributeName,
                    AttributeType = a.AttributeType.ToString(),
                    Value = a.Value
                }).ToList()
        };

        return View("Edit", vm);
    }

    /// <summary>
    /// Maps each nav-reachable public path — exactly as asp-controller/asp-action on _PublicNav and
    /// _PublicFooter's Home/About/Spaces/Menu/Events/Contact/Careers links generates it — to that
    /// page's Cms edit URL. Only pages that actually exist as rows are included, so a page that's
    /// missing (the way Menu itself was until sql/add_menu_page.sql was run) simply falls through to
    /// the live public site rather than 404ing on a Cms url with no matching page id.
    /// </summary>
    private async Task<Dictionary<string, string>> BuildPreviewNavMapAsync(string langCode)
    {
        var navPathsByPageName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Home"] = "/",
            ["About"] = "/about",
            ["Spaces"] = "/spaces",
            ["Menu"] = "/menu",
            ["Events"] = "/events",
            ["Contact"] = "/contact",
            ["Careers"] = "/careers"
        };

        var pages = await _pages.GetAllPagesAsync();
        var map = new Dictionary<string, string>();
        foreach (var page in pages)
        {
            if (!navPathsByPageName.TryGetValue(page.PageName, out var path))
                continue;

            map[path] = Url.Action(nameof(Edit), "Pages", new { area = "Cms", id = page.Id, lang = langCode })!;
        }

        return map;
    }

    /// <summary>
    /// Saves the page's meta fields and upserts every attribute row present in the form —
    /// existing rows (by AttributeName) get updated in place, new ones get inserted. Rows with a
    /// blank AttributeName (unfilled "add new" rows) are ignored. There is no delete yet — remove
    /// an attribute's value to blank it out, or ask an Admin to remove the row directly.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(PageEditViewModel model)
    {
        await _pages.UpdatePageMetaAsync(model.PageId, model.MetaTitle, model.MetaDesc, model.MetaKeyword);

        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in model.Attributes)
        {
            if (string.IsNullOrWhiteSpace(row.AttributeName))
                continue;

            if (!seenNames.Add(row.AttributeName.Trim()))
            {
                TempData["Error"] = $"Attribute name '{row.AttributeName}' was submitted more than once — only the first was saved.";
                continue;
            }

            if (!Enum.TryParse<PageAttributeType>(row.AttributeType, out var type))
                type = PageAttributeType.Text;

            await _pages.SetAttributeAsync(new SetPageAttributeRequest
            {
                PageId = model.PageId,
                AttributeName = row.AttributeName.Trim(),
                AttributeType = type,
                LangId = model.LangId,
                Value = row.Value
            });
        }

        TempData["Success"] = "Page saved.";
        return RedirectToAction(nameof(Edit), new { id = model.PageId, lang = model.LangCode });
    }

    /// <summary>Upserts a single attribute — used by the visual page replicas' edit-field modal (one field, one save, no page reload).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveField(int pageId, int langId, string attributeName, string attributeType, string? value)
    {
        if (string.IsNullOrWhiteSpace(attributeName))
            return BadRequest(new { error = "Attribute name is required." });

        if (!Enum.TryParse<PageAttributeType>(attributeType, out var type))
            type = PageAttributeType.Text;

        await _pages.SetAttributeAsync(new SetPageAttributeRequest
        {
            PageId = pageId,
            AttributeName = attributeName.Trim(),
            AttributeType = type,
            LangId = langId,
            Value = value
        });

        return Json(new { success = true, value });
    }

    /// <summary>Uploads an image for an Image-type attribute and returns its public URL — pasted into the attribute's value field by the page's JS, not auto-saved.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxImageSizeBytes + 1024)]
    public async Task<IActionResult> UploadImage(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded." });

        if (file.Length > MaxImageSizeBytes)
            return BadRequest(new { error = "Image must be smaller than 5 MB." });

        var fileError = await FileTypeValidator.ValidateAsync(file, "Image", FileTypeValidator.ImageExtensions, FileTypeValidator.ImageContentTypes, cancellationToken);
        if (fileError is not null)
            return BadRequest(new { error = fileError });

        var extension = Path.GetExtension(file.FileName);
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var uploadsFolder = Path.Combine(webRoot, "img", "cms");
        Directory.CreateDirectory(uploadsFolder);

        var uniqueFileName = $"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}_{Path.GetFileNameWithoutExtension(file.FileName)}{extension}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        var scanResult = await ScanAndSaveAsync(file, filePath, "image", cancellationToken);
        if (scanResult.Error is not null)
            return scanResult.IsServerFault ? StatusCode(500, new { error = scanResult.Error }) : BadRequest(new { error = scanResult.Error });

        return Json(new { url = $"/img/cms/{uniqueFileName}" });
    }

    /// <summary>Uploads a replacement menu PDF and returns its public URL — saved into customer.page_attributes (Global/menu_pdf_url) by the Menu edit screen's JS via a follow-up SaveField call.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxPdfSizeBytes + 1024)]
    public async Task<IActionResult> UploadMenuPdf(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded." });

        if (file.Length > MaxPdfSizeBytes)
            return BadRequest(new { error = "PDF must be smaller than 20 MB." });

        var fileError = await FileTypeValidator.ValidateAsync(file, "PDF", FileTypeValidator.PdfExtensions, FileTypeValidator.PdfContentTypes, cancellationToken);
        if (fileError is not null)
            return BadRequest(new { error = fileError });

        var extension = Path.GetExtension(file.FileName);
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var uploadsFolder = Path.Combine(webRoot, "pdf", "cms");
        Directory.CreateDirectory(uploadsFolder);

        var uniqueFileName = $"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}_{Path.GetFileNameWithoutExtension(file.FileName)}{extension}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        var scanResult = await ScanAndSaveAsync(file, filePath, "PDF", cancellationToken);
        if (scanResult.Error is not null)
            return scanResult.IsServerFault ? StatusCode(500, new { error = scanResult.Error }) : BadRequest(new { error = scanResult.Error });

        return Json(new { url = $"/pdf/cms/{uniqueFileName}" });
    }

    private readonly record struct ScanAndSaveResult(string? Error, bool IsServerFault);

    /// <summary>
    /// Writes an already type-validated upload to a temp file, scans it for malware, and only then
    /// moves it to <paramref name="destinationPath"/> — a public wwwroot location. The file never
    /// touches a web-servable path before it's been scanned clean. A scan hit or an unavailable
    /// scanner is the caller's fault to fix (bad file / try again); a failure moving the already-clean
    /// file into place is this server's fault — <see cref="ScanAndSaveResult.IsServerFault"/>
    /// distinguishes the two so the controller can return 400 vs 500 accordingly.
    /// </summary>
    private async Task<ScanAndSaveResult> ScanAndSaveAsync(IFormFile file, string destinationPath, string kindLabel, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(ScanTempDirectory);
        var tempPath = Path.Combine(ScanTempDirectory, $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}");

        try
        {
            await using (var tempStream = new FileStream(tempPath, FileMode.Create))
            {
                await file.CopyToAsync(tempStream, cancellationToken);
            }

            var outcome = await _virusScanner.ScanAsync(tempPath, cancellationToken);
            if (outcome != VirusScanOutcome.Clean)
            {
                _logger.LogWarning("Cms {Kind} upload {FileName} rejected by virus scan: {Outcome}.", kindLabel, file.FileName, outcome);
                var message = outcome == VirusScanOutcome.Infected
                    ? $"This {kindLabel} was flagged by a virus scan and could not be accepted."
                    : $"We couldn't scan this {kindLabel} right now. Please try again shortly.";
                return new ScanAndSaveResult(message, IsServerFault: false);
            }

            try
            {
                System.IO.File.Move(tempPath, destinationPath, overwrite: false);
                return new ScanAndSaveResult(null, IsServerFault: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save scanned Cms {Kind} upload {FileName} to {Destination}.", kindLabel, file.FileName, destinationPath);
                return new ScanAndSaveResult($"Failed to save the uploaded {kindLabel}.", IsServerFault: true);
            }
        }
        finally
        {
            if (System.IO.File.Exists(tempPath))
                System.IO.File.Delete(tempPath);
        }
    }
}
