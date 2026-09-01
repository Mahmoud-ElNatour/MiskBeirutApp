using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Application.Dtos.Contact;
using MiskBeirut.Application.Managers;
using MiskBeirut.Core.Repositories;
using MiskBeirut.Web.Areas.Customer.Models;
using MiskBeirut.Web.Support;

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
        ViewData["Reasons"] = await _reasons.GetActiveAsync(CurrentLangCode);
        return View(content);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(ContactInquiryRequest request, CancellationToken cancellationToken)
    {
        var t = new PublicMessages(CurrentLangCode);

        if (!ModelState.IsValid)
        {
            // Per-field, not one blanket "fill in all required fields": every field on this form WAS
            // filled in when this last fired in testing — what actually failed was the FORMAT of one
            // of them (letters typed into the phone box), and a "required fields" message sends the
            // visitor looking for an empty box that doesn't exist.
            var errors = ModelState
                .Where(entry => entry.Value is { Errors.Count: > 0 })
                .ToDictionary(entry => entry.Key, entry => DescribeInvalidField(entry.Key, t));

            return BadRequest(new
            {
                message = t.Pick("Please check the highlighted fields and try again.", "يرجى مراجعة الحقول المحددة والمحاولة مرة أخرى."),
                errors
            });
        }

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

    private static string DescribeInvalidField(string field, PublicMessages t) => field switch
    {
        nameof(ContactInquiryRequest.FullName) => t.Pick("Please enter your full name.", "يرجى إدخال اسمك الكامل."),
        nameof(ContactInquiryRequest.PhoneNumber) => t.Pick("Please enter a valid phone number — digits only, e.g. +961 3 123 456.", "يرجى إدخال رقم هاتف صحيح — أرقام فقط، مثال: ‎+961 3 123 456."),
        nameof(ContactInquiryRequest.Email) => t.Pick("Please enter a valid email address, e.g. name@example.com.", "يرجى إدخال بريد إلكتروني صحيح، مثال: name@example.com."),
        nameof(ContactInquiryRequest.Message) => t.Pick("Please enter your message.", "يرجى كتابة رسالتك."),
        nameof(ContactInquiryRequest.ReasonId) => t.Pick("Please select a reason for contact.", "يرجى اختيار سبب التواصل."),
        _ => t.Pick("Please check this field and try again.", "يرجى مراجعة هذا الحقل والمحاولة مرة أخرى.")
    };
}
