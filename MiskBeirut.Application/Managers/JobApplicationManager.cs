using Microsoft.Extensions.Logging;
using MiskBeirut.Application.Dtos.Careers;
using MiskBeirut.Application.Emails;
using MiskBeirut.Application.Services;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Repositories;

namespace MiskBeirut.Application.Managers;

/// <summary>Job applications submitted via the public Careers page.</summary>
public class JobApplicationManager
{
    private const string NotificationTo = "miskbeirut0@gmail.com";
    private const string NotificationCc = "careers@miskbeirut.com";

    /// <summary>Replies to an applicant go out as careers@, not the app's default sender, so a reply lands in the team's actual inbox.</summary>
    private const string ApplicantReplyFrom = "careers@miskbeirut.com";

    private readonly IJobApplicationRepository _applications;
    private readonly IVacancyRepository _vacancies;
    private readonly ICvSubmissionService _cvSubmission;
    private readonly IEmailSender _email;
    private readonly PageContentManager _pageContent;
    private readonly ILogger<JobApplicationManager> _logger;

    public JobApplicationManager(
        IJobApplicationRepository applications,
        IVacancyRepository vacancies,
        ICvSubmissionService cvSubmission,
        IEmailSender email,
        PageContentManager pageContent,
        ILogger<JobApplicationManager> logger)
    {
        _applications = applications;
        _vacancies = vacancies;
        _cvSubmission = cvSubmission;
        _email = email;
        _pageContent = pageContent;
        _logger = logger;
    }

    /// <summary>
    /// Stores the CV, then records the application. Throws <see cref="InvalidOperationException"/>
    /// with a user-facing message if the vacancy doesn't exist — nothing is written to the database in
    /// that case. A failure to send the HR notification email does not fail the submission; the
    /// application is the source of truth, the email is a best-effort notification.
    /// </summary>
    public async Task<JobApplicationDto> SubmitAsync(CreateJobApplicationRequest request, Stream cvContent, string cvFileName, CancellationToken cancellationToken = default)
    {
        var vacancy = await _vacancies.GetByIdAsync(request.VacancyId, cancellationToken)
            ?? throw new InvalidOperationException("This position is no longer open.");

        var storedCvFileName = await _cvSubmission.SubmitAsync(cvContent, cvFileName, request.Name, cancellationToken);

        var application = await _applications.AddAsync(new JobApplication
        {
            Name = request.Name,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            Address = request.Address,
            CvUrl = storedCvFileName,
            VacancyId = request.VacancyId,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        var footer = await _pageContent.GetEmailFooterContactAsync(cancellationToken);

        try
        {
            var notificationBody = EmailTemplates.JobApplicationNotification(request.Name, request.PhoneNumber, request.Email, request.Address, vacancy.Title, footer);
            await _email.SendAsync(NotificationTo, $"New Application: {vacancy.Title}", notificationBody, NotificationCc, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send HR notification email for application {ApplicationId}.", application.Id);
        }

        try
        {
            var confirmationBody = EmailTemplates.JobApplicationConfirmation(request.Name, vacancy.Title, footer);
            await _email.SendAsync(request.Email, "We've received your application — Misk Beirut", confirmationBody, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send application confirmation email for application {ApplicationId}.", application.Id);
        }

        return ToDto(application, vacancy.Title);
    }

    /// <summary>All applications, most recent first, with each one's vacancy title resolved — for the Cms review list.</summary>
    public async Task<IReadOnlyList<JobApplicationDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var applications = await _applications.GetAllAsync(cancellationToken);
        var vacancies = await _vacancies.GetAllAsync(cancellationToken);
        var vacancyTitles = vacancies.ToDictionary(v => v.Id, v => v.Title);

        return applications
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => ToDto(a, vacancyTitles.GetValueOrDefault(a.VacancyId)))
            .ToList();
    }

    /// <summary>Opens the applicant's stored CV for viewing. Null if the application or its file doesn't exist.
    /// ContentType is the best guess from the file extension so a browser can render a PDF inline
    /// instead of just downloading it; unrecognized types fall back to a generic binary download.</summary>
    public async Task<(Stream Content, string FileName, string ContentType)?> GetCvAsync(int id, CancellationToken cancellationToken = default)
    {
        var application = await _applications.GetByIdAsync(id, cancellationToken);
        if (application is null)
            return null;

        var stream = await _cvSubmission.OpenReadAsync(application.CvUrl, cancellationToken);
        return stream is null ? null : (stream, application.CvUrl, GuessContentType(application.CvUrl));
    }

    private static string GuessContentType(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".doc" => "application/msword",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        _ => "application/octet-stream"
    };

    /// <summary>
    /// Emails the applicant directly (not a notification about them — a message TO them), sent from
    /// careers@miskbeirut.com so a reply lands in the team's actual inbox rather than the app's
    /// generic sender address.
    /// </summary>
    public async Task SendEmailToApplicantAsync(int id, string subject, string body, CancellationToken cancellationToken = default)
    {
        var application = await _applications.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Application {id} was not found.");

        var footer = await _pageContent.GetEmailFooterContactAsync(cancellationToken);
        var htmlBody = EmailTemplates.StaffMessage(body, footer);
        await _email.SendAsync(application.Email, subject, htmlBody, from: ApplicantReplyFrom, cancellationToken: cancellationToken);
    }

    /// <summary>Marks whether HR has made a hire/reject decision on this application yet.</summary>
    public async Task SetDecisionTakenAsync(int id, bool decisionTaken, CancellationToken cancellationToken = default)
    {
        var application = await _applications.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Application {id} was not found.");

        application.DecisionTaken = decisionTaken;
        await _applications.UpdateAsync(application, cancellationToken);
    }

    /// <summary>Deletes the application record, then best-effort deletes its CV file from disk.</summary>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var application = await _applications.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Application {id} was not found.");

        await _applications.DeleteAsync(application, cancellationToken);

        try
        {
            await _cvSubmission.DeleteAsync(application.CvUrl, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete CV file for application {ApplicationId} ({CvUrl}).", id, application.CvUrl);
        }
    }

    private static JobApplicationDto ToDto(JobApplication application, string? vacancyTitle = null) => new()
    {
        Id = application.Id,
        Name = application.Name,
        PhoneNumber = application.PhoneNumber,
        Email = application.Email,
        Address = application.Address,
        CvUrl = application.CvUrl,
        VacancyId = application.VacancyId,
        VacancyTitle = vacancyTitle,
        CreatedAt = application.CreatedAt,
        DecisionTaken = application.DecisionTaken
    };
}
