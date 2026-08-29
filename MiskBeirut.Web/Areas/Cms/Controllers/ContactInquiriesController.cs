using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Application.Managers;

namespace MiskBeirut.Web.Areas.Cms.Controllers;

/// <summary>Inquiries submitted via the public Contact/Reservations page — review, and follow up over WhatsApp.</summary>
public class ContactInquiriesController : CmsControllerBase
{
    private readonly ContactInquiryManager _inquiries;

    public ContactInquiriesController(ContactInquiryManager inquiries)
    {
        _inquiries = inquiries;
    }

    public async Task<IActionResult> Index()
    {
        var inquiries = await _inquiries.GetAllAsync();
        return View(inquiries);
    }

    /// <summary>Sends the configured WhatsApp follow-up template to this inquiry's phone number.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendWhatsApp(int id)
    {
        try
        {
            await _inquiries.SendWhatsAppFollowUpAsync(id, CurrentUserId, CurrentUsername);
            TempData["Success"] = "WhatsApp message sent.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to send WhatsApp message: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDone(int id, bool isDone)
    {
        await _inquiries.SetDoneAsync(id, isDone);
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
            await _inquiries.SendEmailToInquirerAsync(id, subject, body);
            TempData["Success"] = "Email sent.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to send email: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }
}
