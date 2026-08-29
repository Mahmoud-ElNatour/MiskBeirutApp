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

    public MailgunEmailSender(HttpClient http, string domain, string apiKey, string fromAddress)
    {
        _http = http;
        _domain = domain;
        _apiKey = apiKey;
        _fromAddress = fromAddress;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, string? cc = null, string? from = null, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.mailgun.net/v3/{_domain}/messages");
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
        response.EnsureSuccessStatusCode();
    }
}
