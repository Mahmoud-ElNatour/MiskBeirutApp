namespace MiskBeirut.Core.Entities;

/// <summary>A submission from the public Contact/Reservations page's "Send Inquiry" form.</summary>
public class ContactInquiry
{
    public int Id { get; set; }
    public string FullName { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string? Email { get; set; }
    public string Message { get; set; } = null!;
    public int ReasonId { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Whether staff have followed up on / resolved this inquiry.</summary>
    public bool IsDone { get; set; }

    public InquiryReason Reason { get; set; } = null!;
}
