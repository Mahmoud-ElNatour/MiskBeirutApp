using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Application.Dtos.Contact;
using MiskBeirut.Application.Managers;
using MiskBeirut.Core.Repositories;
using MiskBeirut.Web.Areas.Customer.Models;

namespace MiskBeirut.Web.Areas.Customer.Controllers;

public class ContactController : PublicContentController
{
    private readonly InquiryReasonManager _reasons;
    private readonly ContactInquiryManager _inquiries;

    public ContactController(PageContentManager pages, ILanguageRepository languages, InquiryReasonManager reasons, ContactInquiryManager inquiries)
        : base(pages, languages)
    {
        _reasons = reasons;
        _inquiries = inquiries;
    }

    public async Task<IActionResult> Index()
    {
        var content = await LoadPageAsync("Contact");
        ViewData["Reasons"] = await _reasons.GetActiveAsync();
        return View(content);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(ContactInquiryRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { message = "Please fill in all required fields." });

        try
        {
            await _inquiries.SubmitAsync(new CreateContactInquiryRequest
            {
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber,
                Email = request.Email,
                Message = request.Message,
                ReasonId = request.ReasonId
            }, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        return Ok();
    }
}
