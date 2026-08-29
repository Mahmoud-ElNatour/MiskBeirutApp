namespace MiskBeirut.Core.Constants;

/// <summary>
/// Column length limits for the person-identifying fields shared across contact inquiries, job
/// applications, website leads, customers, and employees. One source of truth so the database
/// schema (<c>MiskBeirutDbContext</c>), server-side validation (<c>[StringLength]</c> on the Web
/// layer's request/view models), and the HTML forms' <c>maxlength</c> attributes can never drift
/// out of sync with each other.
/// </summary>
public static class FieldLengths
{
    public const int Name = 200;
    public const int PhoneNumber = 50;
    public const int Email = 256;
    public const int Address = 500;
    public const int Position = 100;
    public const int Message = 2000;
}
