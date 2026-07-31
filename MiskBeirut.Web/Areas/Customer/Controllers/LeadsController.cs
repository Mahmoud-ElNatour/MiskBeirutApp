using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Application.Managers;
using MiskBeirut.Web.Areas.Customer.Models;

namespace MiskBeirut.Web.Areas.Customer.Controllers;

[Area("Customer")]
public class LeadsController : Controller
{
    private readonly WebsiteLeadManager _leads;

    public LeadsController(WebsiteLeadManager leads)
    {
        _leads = leads;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Subscribe(DiscountLeadRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest();

        var (_, alreadyClaimed) = await _leads.CreateAsync(request.Name, request.PhoneNumber, request.Email, cancellationToken);

        if (alreadyClaimed)
            return Conflict();

        return Ok();
    }
}
