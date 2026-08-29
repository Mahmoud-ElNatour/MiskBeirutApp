using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Application.Dtos.Careers;
using MiskBeirut.Application.Managers;
using MiskBeirut.Core.Repositories;
using MiskBeirut.Web.Areas.Customer.Models;
using MiskBeirut.Web.Support;

namespace MiskBeirut.Web.Areas.Customer.Controllers;

public class CareersController : PublicContentController
{
    private const long MaxCvSizeBytes = 5 * 1024 * 1024; // 5 MB

    private readonly VacancyManager _vacancies;
    private readonly JobApplicationManager _applications;

    public CareersController(PageContentManager pages, ILanguageRepository languages, VacancyManager vacancies, JobApplicationManager applications)
        : base(pages, languages)
    {
        _vacancies = vacancies;
        _applications = applications;
    }

    public async Task<IActionResult> Index()
    {
        var content = await LoadPageAsync("Careers");
        ViewData["Vacancies"] = await _vacancies.GetActiveAsync();
        return View(content);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxCvSizeBytes + 1024)]
    public async Task<IActionResult> Apply(JobApplicationRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { message = "Please fill in all required fields and attach your CV." });

        if (request.Cv.Length == 0 || request.Cv.Length > MaxCvSizeBytes)
            return BadRequest(new { message = "CV must be a non-empty file smaller than 5 MB." });

        var fileError = await FileTypeValidator.ValidateAsync(request.Cv, "CV", FileTypeValidator.PdfExtensions, FileTypeValidator.PdfContentTypes, cancellationToken);
        if (fileError is not null)
            return BadRequest(new { message = fileError });

        try
        {
            await using var stream = request.Cv.OpenReadStream();
            await _applications.SubmitAsync(new CreateJobApplicationRequest
            {
                Name = request.Name,
                PhoneNumber = request.PhoneNumber,
                Email = request.Email,
                Address = request.Address,
                VacancyId = request.VacancyId
            }, stream, request.Cv.FileName, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        return Ok();
    }
}
