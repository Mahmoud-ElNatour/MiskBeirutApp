using System.Text;
using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Application.Dtos.Leads;
using MiskBeirut.Application.Managers;

namespace MiskBeirut.Web.Areas.Cms.Controllers;

/// <summary>Leads captured by the website's discount popup — review and export.</summary>
public class WebsiteLeadsController : CmsControllerBase
{
    private readonly WebsiteLeadManager _leads;

    public WebsiteLeadsController(WebsiteLeadManager leads)
    {
        _leads = leads;
    }

    public async Task<IActionResult> Index()
    {
        var leads = await _leads.GetAllAsync();
        return View(leads);
    }

    /// <summary>CSV download — opens directly in Excel. A UTF-8 BOM is included so names with Arabic
    /// characters render correctly instead of as mojibake (Excel guesses ANSI without it).</summary>
    [HttpGet]
    public async Task<IActionResult> Export()
    {
        var leads = await _leads.GetAllAsync();
        var csv = BuildCsv(leads);

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
        return File(bytes, "text/csv", $"website_leads_{DateTime.UtcNow:yyyy-MM-dd}.csv");
    }

    private static string BuildCsv(IEnumerable<WebsiteLeadDto> leads)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(",", new[] { "Name", "Phone", "Email", "Created At" }.Select(EscapeCsv)));

        foreach (var lead in leads)
        {
            builder.AppendLine(string.Join(",", new[]
            {
                lead.Name,
                lead.PhoneNumber,
                lead.Email,
                lead.CreatedAt.ToString("yyyy-MM-dd HH:mm")
            }.Select(EscapeCsv)));
        }

        return builder.ToString();
    }

    private static string EscapeCsv(string? value)
    {
        if (value is null) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
