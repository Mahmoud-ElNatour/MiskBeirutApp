using System.Net;
using System.Text;

namespace MiskBeirut.Application.Emails;

/// <summary>
/// The phone/email shown in every email's footer. Callers source this from the same CMS-managed
/// "Global" page content the site footer reads (see <c>PageContentManager.GetEmailFooterContactAsync</c>);
/// <see cref="Default"/> exists only as a last-resort fallback if that lookup fails or the CMS values
/// haven't been set yet.
/// </summary>
public sealed record EmailFooterContact(string Phone, string Email)
{
    public static EmailFooterContact Default { get; } = new("+961 1 234 567", "concierge@miskbeirut.com");
}

/// <summary>
/// Branded HTML bodies for every transactional email the app sends (contact inquiries, job
/// applications, website leads). Markup is a table-based layout with inline styles — the style
/// email clients that ignore &lt;style&gt; blocks (Outlook desktop, etc.) still render correctly.
/// Every dynamic value is HTML-encoded before interpolation, since much of this content originates
/// from public, unauthenticated form submissions.
/// </summary>
public static class EmailTemplates
{
    private const string Primary = "#355970";
    private const string BrickRed = "#8F383A";
    private const string SlateBlue = "#4E7289";
    private const string MarbleWhite = "#F5F5F6";
    private const string OnyxText = "#1A1A1A";
    private const string BodyFont = "'Hanken Grotesk', 'Segoe UI', Helvetica, Arial, sans-serif";

    /// <summary>
    /// Absolute URL required — email clients load images straight from the network, not from the
    /// app's own static file middleware, so a root-relative path like "/img/logo.png" would break.
    /// </summary>
    private const string LogoUrl = "https://miskbeirut.com/img/logo.png";

    private static string E(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    // ---- Internal staff notifications -------------------------------------

    /// <summary>Notifies the team of a new Contact/Reservations page submission.</summary>
    public static string ContactInquiryNotification(string fullName, string phoneNumber, string? email, string reasonName, string message, EmailFooterContact footer)
    {
        var body = $"""
            {Eyebrow("New Contact Inquiry")}
            <p style="margin:0 0 16px; font-size:15px;">A new inquiry was submitted via the Contact/Reservations page.</p>
            {DetailsTable(
                ("Name", fullName),
                ("Phone", phoneNumber),
                ("Email", string.IsNullOrWhiteSpace(email) ? "—" : email),
                ("Reason", reasonName))}
            {MessageBlock(message)}
            """;
        return Layout("New contact inquiry from " + fullName, body, footer, Primary);
    }

    /// <summary>Notifies the team of a new Careers page application.</summary>
    public static string JobApplicationNotification(string name, string phoneNumber, string email, string? address, string vacancyTitle, EmailFooterContact footer)
    {
        var body = $"""
            {Eyebrow("New Job Application")}
            <p style="margin:0 0 16px; font-size:15px;">A new application was submitted for <strong>{E(vacancyTitle)}</strong>.</p>
            {DetailsTable(
                ("Name", name),
                ("Phone", phoneNumber),
                ("Email", email),
                ("Address", string.IsNullOrWhiteSpace(address) ? "—" : address),
                ("Position", vacancyTitle))}
            """;
        return Layout("New application for " + vacancyTitle, body, footer, Primary);
    }

    /// <summary>Notifies the team of a new discount popup signup.</summary>
    public static string WebsiteLeadNotification(string name, string phoneNumber, string email, EmailFooterContact footer)
    {
        var body = $"""
            {Eyebrow("New Website Lead")}
            <p style="margin:0 0 16px; font-size:15px;">A new discount signup came in from the website.</p>
            {DetailsTable(
                ("Name", name),
                ("Phone", phoneNumber),
                ("Email", email),
                ("Discount", "10% off"))}
            """;
        return Layout("New website lead: " + name, body, footer, Primary);
    }

    // ---- Customer-facing confirmations -------------------------------------

    /// <summary>Confirms receipt of a Contact/Reservations inquiry to the person who submitted it.</summary>
    public static string ContactInquiryConfirmation(string fullName, EmailFooterContact footer)
    {
        var body = $"""
            <p style="margin:0 0 16px; font-size:15px;">Hi {E(fullName)},</p>
            <p style="margin:0 0 16px; font-size:15px;">Thank you for reaching out to Misk Beirut. Your inquiry has been well received, and we'll be in touch soon.</p>
            {Signature()}
            """;
        return Layout("We've received your inquiry", body, footer);
    }

    /// <summary>Confirms receipt of a Careers page application to the applicant.</summary>
    public static string JobApplicationConfirmation(string name, string vacancyTitle, EmailFooterContact footer)
    {
        var body = $"""
            <p style="margin:0 0 16px; font-size:15px;">Hi {E(name)},</p>
            <p style="margin:0 0 16px; font-size:15px;">Thank you for applying for the <strong>{E(vacancyTitle)}</strong> position at Misk Beirut. Your application has been well received, and we'll get in touch soon.</p>
            {Signature()}
            """;
        return Layout("We've received your application", body, footer);
    }

    /// <summary>Confirms a discount signup and states the reward, to the person who signed up.</summary>
    public static string WebsiteLeadConfirmation(string name, string phoneNumber, EmailFooterContact footer)
    {
        var body = $"""
            <p style="margin:0 0 16px; font-size:15px;">Hi {E(name)},</p>
            <p style="margin:0 0 16px; font-size:15px;">Thank you for subscribing! You've claimed {CalloutInline("10% off")} your next visit to Misk Beirut.</p>
            <p style="margin:0 0 16px; font-size:15px;">Simply mention this email or your registered phone number ({E(phoneNumber)}) when you arrive to redeem your discount.</p>
            <p style="margin:0 0 16px; font-size:15px;">We look forward to welcoming you!</p>
            {Signature()}
            """;
        return Layout("Your 10% discount at Misk Beirut", body, footer, BrickRed);
    }

    // ---- Freeform staff replies ---------------------------------------------

    /// <summary>
    /// Wraps a staff-composed freeform message (e.g. a reply to an inquirer or applicant) in the
    /// branded letterhead. The message text is HTML-encoded and its line breaks preserved — staff
    /// write the greeting and sign-off themselves, so nothing else is added around it.
    /// </summary>
    public static string StaffMessage(string bodyText, EmailFooterContact footer)
    {
        var encoded = E(bodyText).Replace("\n", "<br/>");
        var body = $"""<p style="margin:0; font-size:15px;">{encoded}</p>""";
        return Layout("A message from Misk Beirut", body, footer);
    }

    // ---- Shared layout pieces -----------------------------------------------

    private static string Layout(string preheader, string bodyContentHtml, EmailFooterContact footer, string accentColor = BrickRed)
    {
        var year = DateTime.UtcNow.Year;
        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
            <meta charset="utf-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1.0" />
            <title>Misk Beirut</title>
            </head>
            <body style="margin:0; padding:0; background-color:{MarbleWhite}; font-family:{BodyFont};">
            <div style="display:none; max-height:0; overflow:hidden; opacity:0;">{E(preheader)}</div>
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background-color:{MarbleWhite}; padding:32px 16px;">
              <tr>
                <td align="center">
                  <table role="presentation" width="600" cellpadding="0" cellspacing="0" style="max-width:600px; width:100%; background-color:#ffffff; border-top:4px solid {accentColor};">
                    <tr>
                      <td style="padding:32px 40px 24px; text-align:center;">
                        <img src="{LogoUrl}" alt="Misk Beirut" width="160" height="64" style="display:block; margin:0 auto; border:0; outline:none; max-width:160px; height:auto;" />
                      </td>
                    </tr>
                    <tr>
                      <td style="padding:0 40px 40px; color:{OnyxText}; font-family:{BodyFont}; line-height:1.6;">
                        {bodyContentHtml}
                      </td>
                    </tr>
                    <tr>
                      <td style="padding:24px 40px; background-color:{MarbleWhite}; border-top:1px solid #e2e2e4;">
                        <p style="margin:0 0 4px; font-size:12px; color:{SlateBlue};">{E(footer.Phone)} &nbsp;&middot;&nbsp; {E(footer.Email)}</p>
                        <p style="margin:0; font-size:12px; color:{SlateBlue}; opacity:0.7;">&copy; {year} Misk Beirut. All rights reserved.</p>
                      </td>
                    </tr>
                  </table>
                </td>
              </tr>
            </table>
            </body>
            </html>
            """;
    }

    private static string Eyebrow(string text) =>
        $"""<p style="margin:0 0 8px; font-size:12px; font-weight:600; letter-spacing:1px; text-transform:uppercase; color:{BrickRed};">{E(text)}</p>""";

    private static string CalloutInline(string text) =>
        $"""<strong style="color:{BrickRed};">{E(text)}</strong>""";

    private static string Signature() =>
        $"""<p style="margin:24px 0 0; font-size:15px;">&mdash; Misk Beirut</p>""";

    private static string DetailsTable(params (string Label, string Value)[] rows)
    {
        var sb = new StringBuilder();
        sb.Append("""<table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="margin:8px 0 16px; border-collapse:collapse;">""");
        foreach (var (label, value) in rows)
        {
            sb.Append($"""
                <tr>
                  <td style="padding:10px 12px; background-color:{MarbleWhite}; font-size:11px; font-weight:600; text-transform:uppercase; letter-spacing:0.5px; color:{SlateBlue}; width:110px; border-bottom:1px solid #eceef0; vertical-align:top;">{E(label)}</td>
                  <td style="padding:10px 12px; font-size:14px; color:{OnyxText}; border-bottom:1px solid #eceef0;">{E(value)}</td>
                </tr>
                """);
        }
        sb.Append("</table>");
        return sb.ToString();
    }

    private static string MessageBlock(string message)
    {
        var encoded = E(message).Replace("\n", "<br/>");
        return $"""
            <p style="margin:0 0 6px; font-size:11px; font-weight:600; text-transform:uppercase; letter-spacing:0.5px; color:{SlateBlue};">Message</p>
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="border-collapse:collapse;">
              <tr>
                <td style="padding:12px 16px; background-color:{MarbleWhite}; border-left:3px solid {BrickRed}; font-size:14px; color:{OnyxText};">{encoded}</td>
              </tr>
            </table>
            """;
    }
}
