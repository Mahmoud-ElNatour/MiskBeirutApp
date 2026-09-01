namespace MiskBeirut.Core.Entities;

/// <summary>A selectable reason on the public Contact/Reservations form's "Reason for Contact" dropdown.</summary>
public class InquiryReason
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;

    /// <summary>Arabic label shown when the visitor is browsing in Arabic. Falls back to <see cref="Name"/> when unset.</summary>
    public string? NameAr { get; set; }

    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
}
