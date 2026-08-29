namespace MiskBeirut.Application.Dtos.Contact;

public sealed record InquiryReasonDto
{
    public int Id { get; init; }
    public string Name { get; init; } = null!;
}
