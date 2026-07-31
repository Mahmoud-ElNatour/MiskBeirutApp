namespace MiskBeirut.Core.Entities;

/// <summary>customer.pages — PageName is unique.</summary>
public class Page
{
    public int Id { get; set; }
    public string PageName { get; set; } = null!;
    public string? MetaTitle { get; set; }
    public string? MetaDesc { get; set; }
    public string? MetaKeyword { get; set; }

    public ICollection<PageAttribute> Attributes { get; set; } = new List<PageAttribute>();
}
