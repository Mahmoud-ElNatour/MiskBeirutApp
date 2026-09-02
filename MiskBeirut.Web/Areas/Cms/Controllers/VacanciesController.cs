using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Application.Managers;
using MiskBeirut.Web.Areas.Cms.Models.Vacancies;

namespace MiskBeirut.Web.Areas.Cms.Controllers;

/// <summary>
/// The open positions listed on the public Careers page. Vacancies live in their own table rather
/// than in customer.page_attributes (a job posting is a record with its own lifecycle, not a piece
/// of page copy), which is why they get a controller of their own instead of appearing in the
/// Pages editor.
/// </summary>
public class VacanciesController : CmsControllerBase
{
    private readonly VacancyManager _vacancies;

    public VacanciesController(VacancyManager vacancies)
    {
        _vacancies = vacancies;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var vacancies = await _vacancies.GetAllForAdminAsync(cancellationToken);
        return View(vacancies);
    }

    /// <summary>New-vacancy form — the same view as <see cref="Edit"/>, over an empty active vacancy.</summary>
    public IActionResult Create()
    {
        ViewData["IsNew"] = true;
        return View("Edit", new VacancyEditViewModel { IsActive = true });
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var vacancy = await _vacancies.GetForAdminAsync(id, cancellationToken);
        if (vacancy is null)
            return NotFound();

        ViewData["IsNew"] = false;
        ViewData["ApplicationCount"] = vacancy.ApplicationCount;
        return View(VacancyEditViewModel.FromDto(vacancy));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(VacancyEditViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            ViewData["IsNew"] = model.Id == 0;
            return View("Edit", model);
        }

        try
        {
            var saved = await _vacancies.SaveAsync(model.ToRequest(), cancellationToken);
            TempData["Success"] = model.Id == 0 ? $"\"{saved.Title}\" added." : $"\"{saved.Title}\" saved.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            ViewData["IsNew"] = model.Id == 0;
            return View("Edit", model);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActive(int id, bool isActive, CancellationToken cancellationToken)
    {
        await _vacancies.SetActiveAsync(id, isActive, cancellationToken);
        TempData["Success"] = isActive ? "Vacancy is now live on the Careers page." : "Vacancy hidden from the Careers page.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _vacancies.DeleteAsync(id, cancellationToken);
            TempData["Success"] = "Vacancy deleted.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
