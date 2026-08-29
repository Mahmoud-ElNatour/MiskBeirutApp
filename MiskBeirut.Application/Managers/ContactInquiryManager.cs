using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using MiskBeirut.Application.Dtos.Contact;
using MiskBeirut.Application.Emails;
using MiskBeirut.Application.Services;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Repositories;

namespace MiskBeirut.Application.Managers;

/// <summary>Inquiries submitted via the public Contact/Reservations page.</summary>
public class ContactInquiryManager
{
    private const string NotificationTo = "miskbeirut0@gmail.com";
    private const string NotificationCc = "inquiries@miskbeirut.com";

    /// <summary>Replies to the sender go out as inquiries@, not the app's default sender, so a reply lands in the team's actual inbox.</summary>
    private const string InquirerReplyFrom = "inquiries@miskbeirut.com";

    private readonly IContactInquiryRepository _inquiries;
    private readonly IInquiryReasonRepository _reasons;
    private readonly IEmailSender _email;
    private readonly PageContentManager _pageContent;
    private readonly IWhatsAppSender _whatsApp;
    private readonly IContactInquiryWhatsAppMessageRepository _whatsAppMessages;
    private readonly string _whatsAppTemplateName;
    private readonly string _whatsAppTemplateLanguage;
    private readonly string _whatsAppDefaultCountryCode;
    private readonly ILogger<ContactInquiryManager> _logger;

    public ContactInquiryManager(
        IContactInquiryRepository inquiries,
        IInquiryReasonRepository reasons,
        IEmailSender email,
        PageContentManager pageContent,
        IWhatsAppSender whatsApp,
        IContactInquiryWhatsAppMessageRepository whatsAppMessages,
        string whatsAppTemplateName,
        string whatsAppTemplateLanguage,
        string whatsAppDefaultCountryCode,
        ILogger<ContactInquiryManager> logger)
    {
        _inquiries = inquiries;
        _reasons = reasons;
        _email = email;
        _pageContent = pageContent;
        _whatsApp = whatsApp;
        _whatsAppMessages = whatsAppMessages;
        _whatsAppTemplateName = whatsAppTemplateName;
        _whatsAppTemplateLanguage = whatsAppTemplateLanguage;
        _whatsAppDefaultCountryCode = whatsAppDefaultCountryCode;
        _logger = logger;
    }

    /// <summary>
    /// Records the inquiry, then sends a notification to the business and a confirmation to the
    /// sender. A failure to send either email does not fail the submission; the inquiry record is
    /// the source of truth, the emails are best-effort.
    /// </summary>
    public async Task<ContactInquiryDto> SubmitAsync(CreateContactInquiryRequest request, CancellationToken cancellationToken = default)
    {
        var reason = await _reasons.GetByIdAsync(request.ReasonId, cancellationToken)
            ?? throw new InvalidOperationException("Please select a valid reason for contact.");

        var inquiry = await _inquiries.AddAsync(new ContactInquiry
        {
            FullName = request.FullName,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            Message = request.Message,
            ReasonId = request.ReasonId,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        var footer = await _pageContent.GetEmailFooterContactAsync(cancellationToken);

        try
        {
            var notificationBody = EmailTemplates.ContactInquiryNotification(request.FullName, request.PhoneNumber, request.Email, reason.Name, request.Message, footer);
            await _email.SendAsync(NotificationTo, $"New Contact Inquiry: {reason.Name}", notificationBody, NotificationCc, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send contact inquiry notification email for inquiry {InquiryId}.", inquiry.Id);
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                var confirmationBody = EmailTemplates.ContactInquiryConfirmation(request.FullName, footer);
                await _email.SendAsync(request.Email, "We've received your inquiry — Misk Beirut", confirmationBody, cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send inquiry confirmation email for inquiry {InquiryId}.", inquiry.Id);
        }

        return ToDto(inquiry, reason.Name);
    }

    /// <summary>All inquiries, most recent first, each carrying its most recent WhatsApp send status — for the Cms review page.</summary>
    public async Task<IReadOnlyList<ContactInquiryDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var inquiries = await _inquiries.GetAllAsync(cancellationToken);
        var messages = await _whatsAppMessages.GetAllAsync(cancellationToken);
        var lastMessageByInquiry = messages
            .GroupBy(m => m.ContactInquiryId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(m => m.SentAt).First());

        return inquiries
            .OrderByDescending(i => i.CreatedAt)
            .Select(i =>
            {
                lastMessageByInquiry.TryGetValue(i.Id, out var lastMessage);
                return ToDto(i, i.Reason?.Name, lastMessage);
            })
            .ToList();
    }

    /// <summary>
    /// Sends the configured WhatsApp follow-up template to the inquiry's phone number and logs the
    /// attempt (success or failure) so the Cms can show a send history. Unlike the confirmation
    /// emails in <see cref="SubmitAsync"/>, a failure here is surfaced to the caller — the Cms user
    /// clicked "Send" and needs to know it didn't go through.
    /// </summary>
    public async Task<ContactInquiryWhatsAppMessageDto> SendWhatsAppFollowUpAsync(int inquiryId, int? sentByUserId, string? sentByUsername, CancellationToken cancellationToken = default)
    {
        var inquiry = await _inquiries.GetByIdAsync(inquiryId, cancellationToken)
            ?? throw new InvalidOperationException("Inquiry not found.");

        var toPhoneNumber = NormalizeToE164(inquiry.PhoneNumber, _whatsAppDefaultCountryCode);
        var bodyPreview = $"Hi {inquiry.FullName}, thank you for reaching out to Misk Beirut. We've received your inquiry and will get back to you shortly.";

        var log = new ContactInquiryWhatsAppMessage
        {
            ContactInquiryId = inquiry.Id,
            ToPhoneNumber = toPhoneNumber,
            TemplateName = _whatsAppTemplateName,
            Body = bodyPreview,
            SentByUserId = sentByUserId,
            SentByUsername = sentByUsername,
            SentAt = DateTime.UtcNow
        };

        try
        {
            var messageId = await _whatsApp.SendTemplateMessageAsync(
                toPhoneNumber,
                _whatsAppTemplateName,
                _whatsAppTemplateLanguage,
                [inquiry.FullName],
                cancellationToken);

            log.Success = true;
            log.ExternalMessageId = messageId;
        }
        catch (WhatsAppSendException ex)
        {
            log.Success = false;
            log.ErrorMessage = ex.Message;
            await _whatsAppMessages.AddAsync(log, cancellationToken);
            throw;
        }

        await _whatsAppMessages.AddAsync(log, cancellationToken);
        return ToDto(log);
    }

    /// <summary>
    /// Emails the sender directly (not the internal notification — a message TO them), sent from
    /// inquiries@miskbeirut.com so a reply lands in the team's actual inbox. Throws
    /// <see cref="InvalidOperationException"/> if the inquiry has no email on file — the Contact
    /// form only requires a phone number, so this isn't always available.
    /// </summary>
    public async Task SendEmailToInquirerAsync(int id, string subject, string body, CancellationToken cancellationToken = default)
    {
        var inquiry = await _inquiries.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Inquiry {id} was not found.");

        if (string.IsNullOrWhiteSpace(inquiry.Email))
            throw new InvalidOperationException("This inquiry has no email address on file.");

        var footer = await _pageContent.GetEmailFooterContactAsync(cancellationToken);
        var htmlBody = EmailTemplates.StaffMessage(body, footer);
        await _email.SendAsync(inquiry.Email, subject, htmlBody, from: InquirerReplyFrom, cancellationToken: cancellationToken);
    }

    /// <summary>Marks whether staff have followed up on / resolved this inquiry.</summary>
    public async Task SetDoneAsync(int id, bool isDone, CancellationToken cancellationToken = default)
    {
        var inquiry = await _inquiries.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Inquiry {id} was not found.");

        inquiry.IsDone = isDone;
        await _inquiries.UpdateAsync(inquiry, cancellationToken);
    }

    /// <summary>Full WhatsApp send history for one inquiry, most recent first.</summary>
    public async Task<IReadOnlyList<ContactInquiryWhatsAppMessageDto>> GetWhatsAppHistoryAsync(int inquiryId, CancellationToken cancellationToken = default)
    {
        var messages = await _whatsAppMessages.GetByInquiryIdAsync(inquiryId, cancellationToken);
        return messages.Select(ToDto).ToList();
    }

    /// <summary>
    /// Best-effort normalization of a freeform local number into the digits-only E.164 shape the
    /// WhatsApp API requires (e.g. "03 123 456" -&gt; "9613123456"). Numbers already carrying a
    /// country code (a leading '+', "00", or already starting with <paramref name="defaultCountryCode"/>)
    /// are left as entered aside from stripping formatting. This is a heuristic, not validation —
    /// a send that fails because of a bad number surfaces Meta's own rejection message.
    /// </summary>
    internal static string NormalizeToE164(string rawPhoneNumber, string defaultCountryCode)
    {
        var digits = Regex.Replace(rawPhoneNumber, @"[^\d+]", "");

        if (digits.StartsWith("00"))
            digits = digits[2..];
        else if (digits.StartsWith("+"))
            digits = digits[1..];

        if (digits.StartsWith(defaultCountryCode))
            return digits;

        if (digits.StartsWith("0"))
            digits = digits[1..];

        return defaultCountryCode + digits;
    }

    private static ContactInquiryDto ToDto(ContactInquiry inquiry, string? reasonName, ContactInquiryWhatsAppMessage? lastWhatsAppMessage = null) => new()
    {
        Id = inquiry.Id,
        FullName = inquiry.FullName,
        PhoneNumber = inquiry.PhoneNumber,
        Email = inquiry.Email,
        Message = inquiry.Message,
        ReasonId = inquiry.ReasonId,
        ReasonName = reasonName,
        CreatedAt = inquiry.CreatedAt,
        IsDone = inquiry.IsDone,
        LastWhatsAppSentAt = lastWhatsAppMessage?.SentAt,
        LastWhatsAppSuccess = lastWhatsAppMessage?.Success
    };

    private static ContactInquiryWhatsAppMessageDto ToDto(ContactInquiryWhatsAppMessage message) => new()
    {
        Id = message.Id,
        ContactInquiryId = message.ContactInquiryId,
        ToPhoneNumber = message.ToPhoneNumber,
        TemplateName = message.TemplateName,
        Body = message.Body,
        Success = message.Success,
        ExternalMessageId = message.ExternalMessageId,
        ErrorMessage = message.ErrorMessage,
        SentByUsername = message.SentByUsername,
        SentAt = message.SentAt
    };
}
