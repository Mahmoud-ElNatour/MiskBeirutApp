namespace MiskBeirut.Application.Dtos.Contact;

public sealed record ContactInquiryDto
{
    public int Id { get; init; }
    public string FullName { get; init; } = null!;
    public string PhoneNumber { get; init; } = null!;
    public string? Email { get; init; }
    public string Message { get; init; } = null!;
    public int ReasonId { get; init; }
    public string? ReasonName { get; init; }
    public DateTime CreatedAt { get; init; }
    public bool IsDone { get; init; }

    /// <summary>Populated only for the Cms listing — when the last WhatsApp follow-up was sent, if any.</summary>
    public DateTime? LastWhatsAppSentAt { get; init; }

    /// <summary>Populated only for the Cms listing — whether that last attempt succeeded.</summary>
    public bool? LastWhatsAppSuccess { get; init; }
}
