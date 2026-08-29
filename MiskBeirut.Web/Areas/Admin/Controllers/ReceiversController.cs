using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Application.Dtos.Receivers;
using MiskBeirut.Application.Managers;
using MiskBeirut.Core.Constants;
using MiskBeirut.Web.Areas.Admin.Models.Receivers;
using MiskBeirut.Web.Authorization;

namespace MiskBeirut.Web.Areas.Admin.Controllers;

[RequirePrivilege("Receivers")]
public class ReceiversController : AdminControllerBase
{
    private readonly ReceiverManager _receivers;
    private readonly ExpenseManager _expenses;
    private readonly AuditLogManager _auditLogs;

    public ReceiversController(ReceiverManager receivers, ExpenseManager expenses, AuditLogManager auditLogs, BackofficePageContentManager pages) : base(pages)
    {
        _receivers = receivers;
        _expenses = expenses;
        _auditLogs = auditLogs;
    }

    public async Task<IActionResult> Index()
    {
        await LoadPageAsync("Receivers");
        var receivers = await _receivers.GetAllAsync();
        var items = new List<ReceiverListItemViewModel>();
        foreach (var receiver in receivers)
        {
            items.Add(new ReceiverListItemViewModel
            {
                Id = receiver.Id,
                Name = receiver.Name,
                PaidAmount = await _expenses.GetTotalPaidByReceiverAsync(receiver.Id)
            });
        }

        return View(items);
    }

    [HttpGet]
    public IActionResult Create() => View(new ReceiverFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReceiverFormViewModel request)
    {
        if (!ModelState.IsValid)
            return View(request);

        var receiver = await _receivers.CreateAsync(new SaveReceiverRequest { Name = request.Name });
        await _auditLogs.LogAsync("Receiver", "Add", receiver.Id.ToString(), CurrentUserId, CurrentUsername,
            $"Created receiver '{receiver.Name}'.", newValues: AuditJson(receiver));
        TempData["Success"] = $"Receiver '{receiver.Name}' created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var receiver = await _receivers.GetByIdAsync(id);
        if (receiver is null)
            return NotFound();

        return View(new ReceiverFormViewModel { Id = receiver.Id, Name = receiver.Name });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ReceiverFormViewModel request)
    {
        if (id != request.Id)
            return BadRequest();

        if (!ModelState.IsValid)
            return View(request);

        var before = await _receivers.GetByIdAsync(id);
        var after = await _receivers.UpdateAsync(id, new SaveReceiverRequest { Name = request.Name });
        await _auditLogs.LogAsync("Receiver", "Update", id.ToString(), CurrentUserId, CurrentUsername,
            $"Updated receiver '{request.Name}'.", oldValues: AuditJson(before), newValues: AuditJson(after));
        TempData["Success"] = "Receiver updated.";
        return RedirectToAction(nameof(Index));
    }

    // --- JSON API used by the ported Areas/Admin/Views/Receivers/Index.cshtml modals ---

    [HttpGet("/api/receivers/{id:int}")]
    public async Task<IActionResult> GetJson(int id)
    {
        var receiver = await _receivers.GetByIdAsync(id);
        if (receiver is null)
            return NotFound();

        return Json(new { id = receiver.Id, name = receiver.Name });
    }

    [HttpPost("/api/receivers")]
    public async Task<IActionResult> CreateJson([FromBody] ReceiverApiRequest request)
    {
        var receiver = await _receivers.CreateAsync(new SaveReceiverRequest { Name = request.Name });
        await _auditLogs.LogAsync("Receiver", "Add", receiver.Id.ToString(), CurrentUserId, CurrentUsername,
            $"Created receiver '{receiver.Name}'.", newValues: AuditJson(receiver));
        return Json(new { status = "success", id = receiver.Id });
    }

    [HttpPut("/api/receivers/{id:int}")]
    public async Task<IActionResult> UpdateJson(int id, [FromBody] ReceiverApiRequest request)
    {
        var before = await _receivers.GetByIdAsync(id);
        if (before is null)
            return NotFound();

        var after = await _receivers.UpdateAsync(id, new SaveReceiverRequest { Name = request.Name });
        await _auditLogs.LogAsync("Receiver", "Update", id.ToString(), CurrentUserId, CurrentUsername,
            $"Updated receiver '{request.Name}'.", oldValues: AuditJson(before), newValues: AuditJson(after));
        return Json(new { status = "success" });
    }

    [HttpDelete("/api/receivers/{id:int}")]
    public async Task<IActionResult> DeleteJson(int id)
    {
        var before = await _receivers.GetByIdAsync(id);

        try
        {
            await _receivers.DeleteAsync(id);
        }
        catch (Exception)
        {
            return Json(new { status = "error", message = "Could not delete this receiver — they likely still have expense history attached." });
        }

        await _auditLogs.LogAsync("Receiver", "Delete", id.ToString(), CurrentUserId, CurrentUsername,
            $"Deleted receiver {id}.", oldValues: AuditJson(before));
        return Json(new { status = "success" });
    }

    [HttpGet("/api/receivers/{id:int}/expenses")]
    public async Task<IActionResult> ExpensesJson(int id, [FromQuery] DateOnly from, [FromQuery] DateOnly to)
    {
        var expenses = await _expenses.GetByReceiverAsync(id, from, to);
        var items = expenses.Select(e => new { date = e.Date.ToString("yyyy-MM-dd"), amount = e.Amount, note = e.Note }).ToList();

        return Json(new { total = items.Sum(e => e.amount), expenses = items });
    }
}
