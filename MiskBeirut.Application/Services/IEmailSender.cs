namespace MiskBeirut.Application.Services;

public interface IEmailSender
{
    /// <param name="from">Overrides the configured default sender (e.g. a department-specific address like careers@miskbeirut.com). Null uses the default.</param>
    Task SendAsync(string to, string subject, string htmlBody, string? cc = null, string? from = null, CancellationToken cancellationToken = default);
}
