using MiskBeirut.Application.Services;

namespace MiskBeirut.Infrastructure.Services;

/// <summary>
/// Stands in for <see cref="MailgunEmailSender"/> when Mailgun:Domain / Mailgun:ApiKey haven't been
/// filled in. Registered instead of refusing to start the app, so an install missing only its email
/// credentials still serves the site — but every send fails loudly with the setting to fix rather
/// than an HTTP status from a request that never had a chance.
/// </summary>
public class NotConfiguredEmailSender : IEmailSender
{
    public Task SendAsync(string to, string subject, string htmlBody, string? cc = null, string? from = null, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(
            "Email sending is not configured on this server. Set Mailgun:Domain (your verified sending domain), " +
            "Mailgun:ApiKey and Mailgun:FromAddress in appsettings.json, then restart the site.");
}
