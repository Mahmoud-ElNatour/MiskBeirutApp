using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Application.Managers;
using MiskBeirut.Web.Areas.Customer.Models;
using MiskBeirut.Web.Support;

namespace MiskBeirut.Web.Areas.Customer.Controllers;

[Area("Customer")]
public class LeadsController : Controller
{
    private const string LangCookieName = "lang";

    private readonly WebsiteLeadManager _leads;

    public LeadsController(WebsiteLeadManager leads)
    {
        _leads = leads;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Subscribe(DiscountLeadRequest request, CancellationToken cancellationToken)
    {
        var t = new PublicMessages(Request.Cookies[LangCookieName]);

        if (!ModelState.IsValid)
        {
            // The popup used to return a bare 400, which the page could only render as a generic
            // "Something went wrong" — leaving the visitor with no idea which of the three boxes the
            // server actually objected to.
            var errors = ModelState
                .Where(entry => entry.Value is { Errors.Count: > 0 })
                .ToDictionary(entry => entry.Key, entry => DescribeInvalidField(entry.Key, t));

            return BadRequest(new
            {
                message = t.Pick("Please check the highlighted fields and try again.", "يرجى مراجعة الحقول المحددة والمحاولة مرة أخرى."),
                errors
            });
        }

        var (_, alreadyClaimed) = await _leads.CreateAsync(request.Name, request.PhoneNumber, request.Email, cancellationToken);

        if (alreadyClaimed)
            return Conflict();

        return Ok();
    }

    private static string DescribeInvalidField(string field, PublicMessages t) => field switch
    {
        nameof(DiscountLeadRequest.Name) => t.Pick("Please enter your name.", "يرجى إدخال اسمك."),
        nameof(DiscountLeadRequest.PhoneNumber) => t.Pick("Please enter a valid phone number — digits only, e.g. +961 3 123 456.", "يرجى إدخال رقم هاتف صحيح — أرقام فقط، مثال: ‎+961 3 123 456."),
        nameof(DiscountLeadRequest.Email) => t.Pick("Please enter a valid email address, e.g. name@example.com.", "يرجى إدخال بريد إلكتروني صحيح، مثال: name@example.com."),
        _ => t.Pick("Please check this field and try again.", "يرجى مراجعة هذا الحقل والمحاولة مرة أخرى.")
    };
}
