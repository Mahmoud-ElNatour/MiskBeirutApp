namespace MiskBeirut.Core.Entities;

/// <summary>
/// A WhatsApp follow-up message sent to a <see cref="ContactInquiry"/> from the Cms, via the Meta
/// WhatsApp Business Platform (Cloud API). Outbound only for now — see remarks on
/// <see cref="MiskBeirut.Application.Services.IWhatsAppSender"/> for the two-way/webhook path this
/// leaves room for later.
/// </summary>
public class ContactInquiryWhatsAppMessage
{
    public int Id { get; set; }
    public int ContactInquiryId { get; set; }

    /// <summary>The number actually dialed (E.164, normalized from the inquiry's freeform PhoneNumber).</summary>
    public string ToPhoneNumber { get; set; } = null!;

    public string TemplateName { get; set; } = null!;

    /// <summary>Rendered preview of what was sent, for display in the Cms — not what's transmitted (the API call sends the template name + parameters).</summary>
    public string Body { get; set; } = null!;

    public bool Success { get; set; }

    /// <summary>Meta's message id (wamid), when the send succeeded.</summary>
    public string? ExternalMessageId { get; set; }

    /// <summary>Meta's error message, when the send failed (e.g. template not approved, number not on WhatsApp).</summary>
    public string? ErrorMessage { get; set; }

    public int? SentByUserId { get; set; }
    public string? SentByUsername { get; set; }
    public DateTime SentAt { get; set; }

    public ContactInquiry ContactInquiry { get; set; } = null!;
}
