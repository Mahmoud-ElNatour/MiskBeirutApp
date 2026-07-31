using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Application.Dtos.Receivers;
using MiskBeirut.Application.Managers;
using MiskBeirut.Core.Constants;
using MiskBeirut.Web.Areas.Admin.Models.Receivers;

namespace MiskBeirut.Web.Areas.Admin.Controllers;

[Authorize(Roles = RoleNames.Admin)]
public class ReceiversController : AdminControllerBase
{
    private readonly ReceiverManager _receivers;
    private readonly AuditLogManager _auditLogs;

    public ReceiversController(ReceiverManager receivers, AuditLogManager auditLogs)
    {
        _receivers = receivers;
        _auditLogs = auditLogs;
    }

    public async Task<IActionResult> Index()
    {
        var receivers = await _receivers.GetAllAsync();
        return View(receivers);
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
        await _auditLogs.LogAsync("Receiver", "Add", receiver.Id.ToString(), CurrentUserId, CurrentUsername, $"Created receiver '{receiver.Name}'.");
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

        await _receivers.UpdateAsync(id, new SaveReceiverRequest { Name = request.Name });
        await _auditLogs.LogAsync("Receiver", "Update", id.ToString(), CurrentUserId, CurrentUsername, $"Updated receiver '{request.Name}'.");
        TempData["Success"] = "Receiver updated.";
        return RedirectToAction(nameof(Index));
    }
}
