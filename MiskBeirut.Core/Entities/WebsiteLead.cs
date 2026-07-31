namespace MiskBeirut.Core.Entities;

/// <summary>
/// customer.customers — leads captured by the website popup.
/// Named WebsiteLead in C# to avoid confusion with the back-office <see cref="Customer"/> entity.
/// </summary>
public class WebsiteLead
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string Email { get; set; } = null!;
    public bool DiscountRedeemed { get; set; }
    public DateTime CreatedAt { get; set; }
}
