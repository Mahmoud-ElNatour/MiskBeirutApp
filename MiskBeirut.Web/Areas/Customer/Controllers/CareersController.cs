using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Application.Dtos.Careers;
using MiskBeirut.Application.Managers;
using MiskBeirut.Application.Services;
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
        ViewData["Vacancies"] = await _vacancies.GetActiveAsync(CurrentLangCode);
        return View(content);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxCvSizeBytes + 1024)]
    public async Task<IActionResult> Apply(JobApplicationRequest request, CancellationToken cancellationToken)
    {
        var t = new PublicMessages(CurrentLangCode);

        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(entry => entry.Value is { Errors.Count: > 0 })
                .ToDictionary(entry => entry.Key, entry => DescribeInvalidField(entry.Key, t));

            return BadRequest(new
            {
                message = t.Pick("Please check the highlighted fields and try again.", "يرجى مراجعة الحقول المحددة والمحاولة مرة أخرى."),
                errors
            });
        }

        if (request.Cv.Length == 0 || request.Cv.Length > MaxCvSizeBytes)
        {
            return BadRequest(new
            {
                message = t.Pick("Your CV must be a non-empty PDF smaller than 5 MB.", "يجب أن تكون سيرتك الذاتية ملف PDF غير فارغ وأصغر من 5 ميغابايت."),
                errors = new Dictionary<string, string> { [nameof(JobApplicationRequest.Cv)] = t.Pick("Your CV must be a non-empty PDF smaller than 5 MB.", "يجب أن تكون سيرتك الذاتية ملف PDF غير فارغ وأصغر من 5 ميغابايت.") }
            });
        }

        var fileError = await FileTypeValidator.ValidateAsync(request.Cv, "CV", FileTypeValidator.PdfExtensions, FileTypeValidator.PdfContentTypes, cancellationToken);
        if (fileError is not null)
        {
            var localizedFileError = t.Pick(fileError, "تعذّر قبول هذا الملف. يرجى إرفاق سيرتك الذاتية بصيغة PDF.");
            return BadRequest(new
            {
                message = localizedFileError,
                errors = new Dictionary<string, string> { [nameof(JobApplicationRequest.Cv)] = localizedFileError }
            });
        }

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
        catch (CvRejectedException ex)
        {
            var message = ex.Outcome == CvSubmissionOutcome.Infected
                ? t.Pick(ex.Message, "تعذّر قبول ملف سيرتك الذاتية بعد فحصه من الفيروسات. يرجى التحقق من الملف والمحاولة مرة أخرى.")
                : t.Pick("We couldn't check your file right now, so it wasn't submitted. Please try again in a few minutes — if it keeps happening, email your CV to careers@miskbeirut.com.",
                         "تعذّر فحص ملفك في الوقت الحالي، لذا لم يتم إرسال الطلب. يرجى المحاولة بعد بضع دقائق — وإن تكرر الأمر، أرسل سيرتك الذاتية إلى careers@miskbeirut.com.");

            return BadRequest(new
            {
                message,
                errors = new Dictionary<string, string> { [nameof(JobApplicationRequest.Cv)] = message }
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        return Ok();
    }

    private static string DescribeInvalidField(string field, PublicMessages t) => field switch
    {
        nameof(JobApplicationRequest.Name) => t.Pick("Please enter your full name.", "يرجى إدخال اسمك الكامل."),
        nameof(JobApplicationRequest.PhoneNumber) => t.Pick("Please enter a valid phone number — digits only, e.g. +961 3 123 456.", "يرجى إدخال رقم هاتف صحيح — أرقام فقط، مثال: ‎+961 3 123 456."),
        nameof(JobApplicationRequest.Email) => t.Pick("Please enter a valid email address, e.g. name@example.com.", "يرجى إدخال بريد إلكتروني صحيح، مثال: name@example.com."),
        nameof(JobApplicationRequest.Address) => t.Pick("Please shorten your address.", "يرجى اختصار العنوان."),
        nameof(JobApplicationRequest.Cv) => t.Pick("Please attach your CV as a PDF.", "يرجى إرفاق سيرتك الذاتية بصيغة PDF."),
        _ => t.Pick("Please check this field and try again.", "يرجى مراجعة هذا الحقل والمحاولة مرة أخرى.")
    };
}
