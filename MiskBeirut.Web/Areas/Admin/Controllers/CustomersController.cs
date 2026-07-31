using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Application.Dtos.Customers;
using MiskBeirut.Application.Managers;
using MiskBeirut.Core.Constants;
using MiskBeirut.Web.Areas.Admin.Models.Customers;

namespace MiskBeirut.Web.Areas.Admin.Controllers;

[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Supervisor}")]
public class CustomersController : AdminControllerBase
{
    private readonly CustomerManager _customers;
    private readonly DailyClosingManager _dailyClosings;
    private readonly AuditLogManager _auditLogs;

    public CustomersController(CustomerManager customers, DailyClosingManager dailyClosings, AuditLogManager auditLogs)
    {
        _customers = customers;
        _dailyClosings = dailyClosings;
        _auditLogs = auditLogs;
    }

    public async Task<IActionResult> Index()
    {
        var customers = await _customers.GetAllAsync();
        return View(customers);
    }

    public async Task<IActionResult> Details(int id)
    {
        var customer = await _customers.GetByIdAsync(id);
        if (customer is null)
            return NotFound();

        var vm = new CustomerDetailsViewModel
        {
            Customer = customer,
            Ledger = (await _customers.GetLedgerAsync(id)).OrderByDescending(l => l.Date).ToList(),
            RecentClosings = (await _dailyClosings.GetAllAsync()).Take(60).ToList(),
            NewEntry = new AddCustomerLedgerEntryViewModel { CustomerId = id }
        };
        return View(vm);
    }

    [HttpGet]
    public IActionResult Create() => View(new CustomerFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerFormViewModel request)
    {
        if (!ModelState.IsValid)
            return View(request);

        var customer = await _customers.CreateAsync(new CreateCustomerRequest
        {
            Name = request.Name,
            PhoneNumber = request.PhoneNumber
        });

        await _auditLogs.LogAsync("Customer", "Add", customer.Id.ToString(), CurrentUserId, CurrentUsername, $"Created customer '{customer.Name}'.");

        TempData["Success"] = $"Customer '{customer.Name}' created.";
        return RedirectToAction(nameof(Details), new { id = customer.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var customer = await _customers.GetByIdAsync(id);
        if (customer is null)
            return NotFound();

        return View(new CustomerFormViewModel { Id = customer.Id, Name = customer.Name, PhoneNumber = customer.PhoneNumber });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CustomerFormViewModel request)
    {
        if (id != request.Id)
            return BadRequest();

        if (!ModelState.IsValid)
            return View(request);

        await _customers.UpdateAsync(id, new UpdateCustomerRequest
        {
            Name = request.Name,
            PhoneNumber = request.PhoneNumber
        });

        await _auditLogs.LogAsync("Customer", "Update", id.ToString(), CurrentUserId, CurrentUsername, $"Updated customer '{request.Name}'.");

        TempData["Success"] = "Customer updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
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
                DailyClosingId = request.DailyClosingId
            });

            await _auditLogs.LogAsync("CustomerLedger", "Add", entry.Id.ToString(), CurrentUserId, CurrentUsername,
                $"Added {entry.Type} {entry.Amount:N2} to customer {request.CustomerId}.");

            TempData["Success"] = "Ledger entry added.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id = request.CustomerId });
    }
}
