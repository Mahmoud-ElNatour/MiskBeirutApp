using Microsoft.Extensions.Logging;
using MiskBeirut.Application.Dtos.Leads;
using MiskBeirut.Application.Emails;
using MiskBeirut.Application.Services;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Repositories;

namespace MiskBeirut.Application.Managers;

/// <summary>Leads captured by the website's discount popup.</summary>
public class WebsiteLeadManager
{
    private const string NotificationTo = "miskbeirut0@gmail.com";
    private const string NotificationCc = "website-lead@miskbeirut.com";

    private readonly IWebsiteLeadRepository _leads;
    private readonly IEmailSender _email;
    private readonly PageContentManager _pageContent;
    private readonly ILogger<WebsiteLeadManager> _logger;

    public WebsiteLeadManager(IWebsiteLeadRepository leads, IEmailSender email, PageContentManager pageContent, ILogger<WebsiteLeadManager> logger)
    {
        _leads = leads;
        _email = email;
        _pageContent = pageContent;
        _logger = logger;
    }

    /// <summary>
    /// Records a new lead and, if this is their first time claiming the discount, sends a
    /// notification to the business and a confirmation to the lead. A failure to send either
    /// email does not fail the submission; the lead record is the source of truth, the emails
    /// are best-effort.
    /// </summary>
    public async Task<(WebsiteLeadDto Lead, bool AlreadyClaimed)> CreateAsync(string name, string phoneNumber, string email, CancellationToken cancellationToken = default)
    {
        var existing = await _leads.GetByEmailAsync(email, cancellationToken)
            ?? await _leads.GetByPhoneNumberAsync(phoneNumber, cancellationToken);
        if (existing is not null)
            return (ToDto(existing), true);

        var lead = await _leads.AddAsync(new WebsiteLead
        {
            Name = name,
            PhoneNumber = phoneNumber,
            Email = email
        }, cancellationToken);

        var footer = await _pageContent.GetEmailFooterContactAsync(cancellationToken);

        try
        {
            var notificationBody = EmailTemplates.WebsiteLeadNotification(name, phoneNumber, email, footer);
            await _email.SendAsync(NotificationTo, $"New Website Lead: {name}", notificationBody, NotificationCc, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send website lead notification email for lead {LeadId}.", lead.Id);
        }

        try
        {
            var confirmationBody = EmailTemplates.WebsiteLeadConfirmation(name, phoneNumber, footer);
            await _email.SendAsync(email, "Your 10% Discount at Misk Beirut", confirmationBody, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send discount confirmation email for lead {LeadId}.", lead.Id);
        }

        return (ToDto(lead), false);
    }

    /// <summary>All leads, most recent first — for the Cms review/export list.</summary>
    public async Task<IReadOnlyList<WebsiteLeadDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var leads = await _leads.GetAllAsync(cancellationToken);
        return leads.OrderByDescending(l => l.CreatedAt).Select(ToDto).ToList();
    }

    private static WebsiteLeadDto ToDto(WebsiteLead lead) => new()
    {
        Id = lead.Id,
        Name = lead.Name,
        PhoneNumber = lead.PhoneNumber,
        Email = lead.Email,
        DiscountRedeemed = lead.DiscountRedeemed,
        CreatedAt = lead.CreatedAt
    };
}
