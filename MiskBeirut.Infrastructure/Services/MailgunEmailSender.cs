using System.Net.Http.Headers;
using System.Text;
using MiskBeirut.Application.Services;

namespace MiskBeirut.Infrastructure.Services;

public class MailgunEmailSender : IEmailSender
{
    private readonly HttpClient _http;
    private readonly string _domain;
    private readonly string _apiKey;
    private readonly string _fromAddress;
    private readonly string _apiBaseUrl;

    /// <param name="apiBaseUrl">
    /// Mailgun API host — "https://api.mailgun.net" for US domains, "https://api.eu.mailgun.net" for
    /// EU ones. A US-hosted request for an EU domain answers 404, so this is configurable rather than
    /// hardcoded. Program.cs guarantees every argument here is non-blank.
    /// </param>
    public MailgunEmailSender(HttpClient http, string domain, string apiKey, string fromAddress, string apiBaseUrl = "https://api.mailgun.net")
    {
        _http = http;
        _domain = domain;
        _apiKey = apiKey;
        _fromAddress = fromAddress;
        _apiBaseUrl = apiBaseUrl.TrimEnd('/');
    }

    public async Task SendAsync(string to, string subject, string htmlBody, string? cc = null, string? from = null, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_apiBaseUrl}/v3/{_domain}/messages");
        var authBytes = Encoding.UTF8.GetBytes($"api:{_apiKey}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

        var fields = new Dictionary<string, string>
        {
            ["from"] = string.IsNullOrWhiteSpace(from) ? _fromAddress : from,
            ["to"] = to,
            ["subject"] = subject,
            ["html"] = htmlBody
        };
        if (!string.IsNullOrWhiteSpace(cc))
            fields["cc"] = cc;

        request.Content = new FormUrlEncodedContent(fields);

        using var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            // Every caller logs-and-continues on a send failure (the record is the source of truth,
            // the email is best-effort), so the exception message is the ONLY place the reason ever
            // appears. EnsureSuccessStatusCode alone gave "Response status code does not indicate
            // success: 401" with no hint that, say, the domain isn't verified or the recipient isn't
            // on a sandbox domain's authorized list — which is why a lead that claimed the discount
            // could silently never receive their email.
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            // 404 is never about the message — it means the URL named no Mailgun domain, so say which
            // domain was tried and on which host rather than leaving "Not Found" to be read as a bug
            // in the sending code.
            var hint = response.StatusCode == System.Net.HttpStatusCode.NotFound
                ? $" Mailgun has no domain '{_domain}' on {_apiBaseUrl} — check Mailgun:Domain, and set Mailgun:ApiBaseUrl to https://api.eu.mailgun.net if the domain is in the EU region."
                : "";

            throw new HttpRequestException(
                $"Mailgun rejected the message to {to} with {(int)response.StatusCode} {response.ReasonPhrase}: {body}{hint}",
                inner: null,
                statusCode: response.StatusCode);
        }
    }
}
