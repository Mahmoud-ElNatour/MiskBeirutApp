using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MiskBeirut.Application.Services;

namespace MiskBeirut.Infrastructure.Services;

/// <summary>Sends WhatsApp template messages via Meta's Graph API (WhatsApp Business Platform / Cloud API).</summary>
public class MetaWhatsAppSender : IWhatsAppSender
{
    private readonly HttpClient _http;
    private readonly string _phoneNumberId;
    private readonly string _accessToken;
    private readonly string _apiVersion;

    public MetaWhatsAppSender(HttpClient http, string phoneNumberId, string accessToken, string apiVersion)
    {
        _http = http;
        _phoneNumberId = phoneNumberId;
        _accessToken = accessToken;
        _apiVersion = apiVersion;
    }

    public async Task<string> SendTemplateMessageAsync(
        string toPhoneNumber,
        string templateName,
        string languageCode,
        IReadOnlyList<string> bodyParameters,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://graph.facebook.com/{_apiVersion}/{_phoneNumberId}/messages");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        var payload = new SendTemplateRequest
        {
            To = toPhoneNumber,
            Template = new TemplatePayload
            {
                Name = templateName,
                Language = new LanguagePayload { Code = languageCode },
                Components = bodyParameters.Count == 0
                    ? []
                    : [
                        new ComponentPayload
                        {
                            Type = "body",
                            Parameters = bodyParameters.Select(p => new ParameterPayload { Text = p }).ToList()
                        }
                    ]
            }
        };
        request.Content = JsonContent.Create(payload);

        using var response = await _http.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = TryExtractErrorMessage(responseBody) ?? $"WhatsApp API request failed ({(int)response.StatusCode} {response.StatusCode}).";
            throw new WhatsAppSendException(errorMessage);
        }

        try
        {
            var result = JsonSerializer.Deserialize<SendTemplateResponse>(responseBody);
            var messageId = result?.Messages?.FirstOrDefault()?.Id;
            if (string.IsNullOrWhiteSpace(messageId))
                throw new WhatsAppSendException("WhatsApp API returned success but no message id.");
            return messageId;
        }
        catch (JsonException ex)
        {
            throw new WhatsAppSendException("WhatsApp API returned an unrecognized response.", ex);
        }
    }

    private static string? TryExtractErrorMessage(string responseBody)
    {
        try
        {
            var error = JsonSerializer.Deserialize<ErrorEnvelope>(responseBody);
            return error?.Error?.Message;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // Request DTOs — see https://developers.facebook.com/docs/whatsapp/cloud-api/reference/messages
    private class SendTemplateRequest
    {
        [JsonPropertyName("messaging_product")]
        public string MessagingProduct { get; set; } = "whatsapp";

        [JsonPropertyName("to")]
        public string To { get; set; } = null!;

        [JsonPropertyName("type")]
        public string Type { get; set; } = "template";

        [JsonPropertyName("template")]
        public TemplatePayload Template { get; set; } = null!;
    }

    private class TemplatePayload
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        [JsonPropertyName("language")]
        public LanguagePayload Language { get; set; } = null!;

        [JsonPropertyName("components")]
        public List<ComponentPayload> Components { get; set; } = [];
    }

    private class LanguagePayload
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = null!;
    }

    private class ComponentPayload
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = null!;

        [JsonPropertyName("parameters")]
        public List<ParameterPayload> Parameters { get; set; } = [];
    }

    private class ParameterPayload
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "text";

        [JsonPropertyName("text")]
        public string Text { get; set; } = null!;
    }

    // Response DTOs
    private class SendTemplateResponse
    {
        [JsonPropertyName("messages")]
        public List<MessageId>? Messages { get; set; }
    }

    private class MessageId
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }

    private class ErrorEnvelope
    {
        [JsonPropertyName("error")]
        public ErrorDetail? Error { get; set; }
    }

    private class ErrorDetail
    {
        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}
