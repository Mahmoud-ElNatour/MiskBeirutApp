namespace MiskBeirut.Application.Dtos.Contact;

public sealed record ContactInquiryWhatsAppMessageDto
{
    public int Id { get; init; }
    public int ContactInquiryId { get; init; }
    public string ToPhoneNumber { get; init; } = null!;
    public string TemplateName { get; init; } = null!;
    public string Body { get; init; } = null!;
    public bool Success { get; init; }
    public string? ExternalMessageId { get; init; }
    public string? ErrorMessage { get; init; }
    public string? SentByUsername { get; init; }
    public DateTime SentAt { get; init; }
}
