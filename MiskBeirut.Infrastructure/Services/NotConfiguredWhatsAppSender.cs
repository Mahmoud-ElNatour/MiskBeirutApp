using MiskBeirut.Application.Services;

namespace MiskBeirut.Infrastructure.Services;

/// <summary>
/// Stands in for <see cref="IWhatsAppSender"/> until WhatsApp:PhoneNumberId, WhatsApp:AccessToken
/// and WhatsApp:TemplateName are set in config. Registered instead of failing app startup, since
/// (unlike Mailgun) this integration is expected to sit unconfigured for a while after the Cms
/// page ships, until the Meta Business app/template are approved — every other Cms/Admin feature
/// should keep working in the meantime. The friendly error only surfaces when someone actually
/// clicks "Send WhatsApp".
/// </summary>
public class NotConfiguredWhatsAppSender : IWhatsAppSender
{
    public Task<string> SendTemplateMessageAsync(
        string toPhoneNumber,
        string templateName,
        string languageCode,
        IReadOnlyList<string> bodyParameters,
        CancellationToken cancellationToken = default)
        => throw new WhatsAppSendException(
            "WhatsApp isn't configured yet. Set WhatsApp:PhoneNumberId, WhatsApp:AccessToken and WhatsApp:TemplateName in appsettings (or user secrets) once your Meta WhatsApp Business app and template are approved.");
}
