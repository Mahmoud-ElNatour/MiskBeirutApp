using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Application.Managers;

namespace MiskBeirut.Web.Areas.Cms.Controllers;

/// <summary>Job applications submitted via the public Careers page — review, mark a hire/reject decision, delete.</summary>
public class JobApplicationsController : CmsControllerBase
{
    private readonly JobApplicationManager _applications;

    public JobApplicationsController(JobApplicationManager applications)
    {
        _applications = applications;
    }

    public async Task<IActionResult> Index()
    {
        var applications = await _applications.GetAllAsync();
        return View(applications);
    }

    /// <summary>Streams the applicant's CV for viewing in the browser — never a public URL, this is the
    /// only way to reach the file (see ICvSubmissionService). No Content-Disposition/filename is set so
    /// browsers render it inline (e.g. a PDF opens in the tab) instead of forcing a download.</summary>
    [HttpGet]
    public async Task<IActionResult> ViewCv(int id)
    {
        var cv = await _applications.GetCvAsync(id);
        if (cv is null)
            return NotFound();

        var (content, _, contentType) = cv.Value;
        return File(content, contentType);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDecision(int id, bool decisionTaken)
    {
        await _applications.SetDecisionTakenAsync(id, decisionTaken);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendEmail(int id, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(body))
        {
            TempData["Error"] = "Subject and message are both required.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _applications.SendEmailToApplicantAsync(id, subject, body);
            TempData["Success"] = "Email sent.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to send email: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _applications.DeleteAsync(id);
        TempData["Success"] = "Application deleted.";
        return RedirectToAction(nameof(Index));
    }
}
