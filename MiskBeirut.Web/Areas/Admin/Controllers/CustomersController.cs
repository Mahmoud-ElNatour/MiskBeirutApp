using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Application.Dtos.Customers;
using MiskBeirut.Application.Managers;
using MiskBeirut.Core.Constants;
using MiskBeirut.Core.Enums;
using MiskBeirut.Web.Areas.Admin.Models.Customers;
using MiskBeirut.Web.Authorization;

namespace MiskBeirut.Web.Areas.Admin.Controllers;

// No class-level [RequirePrivilege]: Credits and Cashbacks are independently grantable pages, not
// implied by the base "Customers" privilege, so every action below is tagged individually instead
// of inheriting one shared class-level requirement.
public class CustomersController : AdminControllerBase
{
    private readonly CustomerManager _customers;
    private readonly DailyClosingManager _dailyClosings;
    private readonly AuditLogManager _auditLogs;

    public CustomersController(CustomerManager customers, DailyClosingManager dailyClosings, AuditLogManager auditLogs, BackofficePageContentManager pages) : base(pages)
    {
        _customers = customers;
        _dailyClosings = dailyClosings;
        _auditLogs = auditLogs;
    }

    [RequirePrivilege("Customers")]
    public async Task<IActionResult> Index()
    {
        await LoadPageAsync("Customers");
        var customers = await _customers.GetAllAsync();
        return View(customers);
    }

    /// <summary>Control Panel report: every Credit ledger entry across all customers.</summary>
    [RequirePrivilege("Credits")]
    public async Task<IActionResult> Credits(int? month, int? year)
    {
        await LoadPageAsync("Credits");
        var records = await _customers.GetLedgerReportAsync(CustomerLedgerType.Credit, month, year);

        ViewData["CurrentMonth"] = month;
        ViewData["CurrentYear"] = year;
        ViewData["Total"] = records.Sum(r => r.Amount);
        ViewData["Count"] = records.Count;
        ViewData["Max"] = records.Count > 0 ? records.Max(r => r.Amount) : 0;

        return View(records);
    }

    /// <summary>Control Panel report: every Cashback ledger entry across all customers.</summary>
    [RequirePrivilege("Cashbacks")]
    public async Task<IActionResult> Cashbacks(int? month, int? year)
    {
        await LoadPageAsync("Cashbacks");
        var records = await _customers.GetLedgerReportAsync(CustomerLedgerType.Cashback, month, year);

        ViewData["CurrentMonth"] = month;
        ViewData["CurrentYear"] = year;
        ViewData["Total"] = records.Sum(r => r.Amount);
        ViewData["Count"] = records.Count;
        ViewData["Max"] = records.Count > 0 ? records.Max(r => r.Amount) : 0;

        return View(records);
    }

    [RequirePrivilege("Customers")]
    public async Task<IActionResult> Details(int id, int? month, int? year)
    {
        var customer = await _customers.GetByIdAsync(id);
        if (customer is null)
            return NotFound();

        var ledger = (await _customers.GetLedgerAsync(id)).OrderByDescending(l => l.Date).AsEnumerable();
        if (month.HasValue)
            ledger = ledger.Where(l => l.Date.Month == month.Value);
        if (year.HasValue)
            ledger = ledger.Where(l => l.Date.Year == year.Value);

        ViewData["CurrentMonth"] = month;
        ViewData["CurrentYear"] = year;

        var vm = new CustomerDetailsViewModel
        {
            Customer = customer,
            Ledger = ledger.ToList(),
            RecentClosings = (await _dailyClosings.GetAllAsync()).Take(60).ToList(),
            NewEntry = new AddCustomerLedgerEntryViewModel { CustomerId = id }
        };
        return View(vm);
    }

    [HttpGet]
    [RequirePrivilege("Customers")]
    public IActionResult Create() => View(new CustomerFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePrivilege("Customers")]
    public async Task<IActionResult> Create(CustomerFormViewModel request)
    {
        if (!ModelState.IsValid)
            return View(request);

        var customer = await _customers.CreateAsync(new CreateCustomerRequest
        {
            Name = request.Name,
            PhoneNumber = request.PhoneNumber
        });

        await _auditLogs.LogAsync("Customer", "Add", customer.Id.ToString(), CurrentUserId, CurrentUsername,
            $"Created customer '{customer.Name}'.", newValues: AuditJson(customer));

        TempData["Success"] = $"Customer '{customer.Name}' created.";
        return RedirectToAction(nameof(Details), new { id = customer.Id });
    }

    [HttpGet]
    [RequirePrivilege("Customers")]
    public async Task<IActionResult> Edit(int id)
    {
        var customer = await _customers.GetByIdAsync(id);
        if (customer is null)
            return NotFound();

        return View(new CustomerFormViewModel { Id = customer.Id, Name = customer.Name, PhoneNumber = customer.PhoneNumber });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePrivilege("Customers")]
    public async Task<IActionResult> Edit(int id, CustomerFormViewModel request)
    {
        if (id != request.Id)
            return BadRequest();

        if (!ModelState.IsValid)
            return View(request);

        var before = await _customers.GetByIdAsync(id);

        var after = await _customers.UpdateAsync(id, new UpdateCustomerRequest
        {
            Name = request.Name,
            PhoneNumber = request.PhoneNumber
        });

        await _auditLogs.LogAsync("Customer", "Update", id.ToString(), CurrentUserId, CurrentUsername,
            $"Updated customer '{request.Name}'.", oldValues: AuditJson(before), newValues: AuditJson(after));

        TempData["Success"] = "Customer updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePrivilege("Customers")]
    public async Task<IActionResult> AddLedgerEntry([Bind(Prefix = "NewEntry")] AddCustomerLedgerEntryViewModel request)
    {
        if (!ModelState.IsValid)
            return RedirectToAction(nameof(Details), new { id = request.CustomerId });

        var signedAmount = request.Type == Core.Enums.CustomerLedgerType.Credit
            ? -Math.Abs(request.Amount)
            : Math.Abs(request.Amount);

        try
        {
            var entry = await _customers.AddLedgerEntryAsync(new CreateCustomerLedgerEntryRequest
            {
                Date = DateOnly.FromDateTime(DateTime.Today),
                Amount = signedAmount,
                Type = request.Type,
                Note = request.Note,
                CustomerId = request.CustomerId,
                DailyClosingId = request.DailyClosingId,
                IsManualEntry = true
            });

            await _auditLogs.LogAsync("CustomerLedger", "Add", entry.Id.ToString(), CurrentUserId, CurrentUsername,
                $"Added {entry.Type} {entry.Amount:N2} to customer {request.CustomerId}.", newValues: AuditJson(entry));

            TempData["Success"] = "Ledger entry added.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id = request.CustomerId });
    }

    // --- JSON API used by the ported Areas/Admin/Views/Customers/Index.cshtml modals ---

    [HttpGet("/api/customers/{id:int}")]
    [RequirePrivilege("Customers")]
    public async Task<IActionResult> GetJson(int id)
    {
        var customer = await _customers.GetByIdAsync(id);
        if (customer is null)
            return NotFound();

        return Json(new { id = customer.Id, username = customer.Name, phoneNumber = customer.PhoneNumber, balance = customer.Balance });
    }

    [HttpPost("/api/customers")]
    [RequirePrivilege("Customers")]
    public async Task<IActionResult> CreateJson([FromBody] CustomerApiRequest request)
    {
        if (!ModelState.IsValid)
            return Json(new { status = "error", message = FirstModelError() });

        var customer = await _customers.CreateAsync(new CreateCustomerRequest { Name = request.Username, PhoneNumber = request.PhoneNumber ?? "" });
        if (request.Balance != 0)
            customer = await _customers.SetBalanceAsync(customer.Id, request.Balance);

        await _auditLogs.LogAsync("Customer", "Add", customer.Id.ToString(), CurrentUserId, CurrentUsername,
            $"Created customer '{customer.Name}'.", newValues: AuditJson(customer));

        return Json(new { status = "success", id = customer.Id });
    }

    [HttpPut("/api/customers/{id:int}")]
    [RequirePrivilege("Customers")]
    public async Task<IActionResult> UpdateJson(int id, [FromBody] CustomerApiRequest request)
    {
        if (!ModelState.IsValid)
            return Json(new { status = "error", message = FirstModelError() });

        var before = await _customers.GetByIdAsync(id);
        if (before is null)
            return NotFound();

        var after = await _customers.UpdateAsync(id, new UpdateCustomerRequest { Name = request.Username, PhoneNumber = request.PhoneNumber ?? "" });
        await _auditLogs.LogAsync("Customer", "Update", id.ToString(), CurrentUserId, CurrentUsername,
            $"Updated customer '{request.Username}'.", oldValues: AuditJson(before), newValues: AuditJson(after));

        // A balance change here isn't a raw overwrite — it's recorded as a Credit/Cashback ledger
        // entry dated today (see AdjustBalanceAsync), so it shows up in the customer's history and
        // the Credits/Cashbacks control-panel reports instead of vanishing silently.
        try
        {
            var note = $"Balance edited on the Customers page by {CurrentUsername ?? "a Cms user"}.";
            await _customers.AdjustBalanceAsync(id, request.Balance, note);
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { status = "error", message = ex.Message });
        }

        return Json(new { status = "success" });
    }

    [HttpDelete("/api/customers/{id:int}")]
    [RequirePrivilege("Customers")]
    public async Task<IActionResult> DeleteJson(int id)
    {
        var before = await _customers.GetByIdAsync(id);

        try
        {
            await _customers.DeleteAsync(id);
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { status = "error", message = ex.Message });
        }
        catch (Exception)
        {
            return Json(new { status = "error", message = "Could not delete this customer — they likely still have ledger history attached." });
        }

        await _auditLogs.LogAsync("Customer", "Delete", id.ToString(), CurrentUserId, CurrentUsername,
            $"Deleted customer {id}.", oldValues: AuditJson(before));
        return Json(new { status = "success" });
    }

    [HttpGet("/api/customers/{id:int}/history")]
    [RequirePrivilege("Customers")]
    public async Task<IActionResult> HistoryJson(int id, [FromQuery] DateOnly from, [FromQuery] DateOnly to)
    {
        var ledger = await _customers.GetLedgerAsync(id);
        var inRange = ledger.Where(l => l.Date >= from && l.Date <= to).ToList();

        var credits = inRange.Where(l => l.Type == CustomerLedgerType.Credit)
            .Select(l => new { date = l.Date.ToString("yyyy-MM-dd"), amount = Math.Abs(l.Amount), note = l.Note })
            .ToList();
        var cashbacks = inRange.Where(l => l.Type == CustomerLedgerType.Cashback)
            .Select(l => new { date = l.Date.ToString("yyyy-MM-dd"), amount = l.Amount, note = l.Note })
            .ToList();

        return Json(new
        {
            totalCredits = credits.Sum(c => c.amount),
            totalCashbacks = cashbacks.Sum(c => c.amount),
            credits,
            cashbacks
        });
    }
}
