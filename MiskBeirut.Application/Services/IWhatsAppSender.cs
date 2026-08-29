namespace MiskBeirut.Application.Services;

/// <summary>
/// Sends outbound WhatsApp messages via the Meta WhatsApp Business Platform (Cloud API) — not the
/// Meta Business Suite inbox, which has no send API and is operated by hand. Reaching someone who
/// hasn't messaged the business number in the last 24 hours (true for every fresh lead) legally
/// requires a pre-approved message template, so this only sends templates, not free-form text.
/// Two-way conversations (reading replies back into the Cms) would need a separate inbound
/// webhook wired up on Meta's side — deliberately out of scope for now; <see cref="ExternalMessageId"/>
/// on the caller's log entry is kept around so that can be correlated later without a redesign.
/// </summary>
public interface IWhatsAppSender
{
    /// <summary>
    /// Sends a template message. <paramref name="toPhoneNumber"/> must already be E.164
    /// (country code, no leading zero, digits only — e.g. "9613123456"). Throws
    /// <see cref="WhatsAppSendException"/> with Meta's own error message on failure (template not
    /// approved, number not reachable on WhatsApp, expired token, etc.) — that message is safe to
    /// surface to a Cms user as-is.
    /// </summary>
    /// <returns>Meta's message id (wamid) for the sent message.</returns>
    Task<string> SendTemplateMessageAsync(
        string toPhoneNumber,
        string templateName,
        string languageCode,
        IReadOnlyList<string> bodyParameters,
        CancellationToken cancellationToken = default);
}

/// <summary>Thrown when the Meta Cloud API rejects a send. <see cref="Message"/> is Meta's own error text.</summary>
public class WhatsAppSendException : Exception
{
    public WhatsAppSendException(string message) : base(message)
    {
    }

    public WhatsAppSendException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
