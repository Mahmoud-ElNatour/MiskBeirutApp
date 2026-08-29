namespace MiskBeirut.Application.Dtos.Contact;

public sealed record CreateContactInquiryRequest
{
    public string FullName { get; init; } = null!;
    public string PhoneNumber { get; init; } = null!;
    public string? Email { get; init; }
    public string Message { get; init; } = null!;
    public int ReasonId { get; init; }
}
