namespace MiskBeirut.Core.Entities;

/// <summary>customer.languages — Code is unique (e.g. "en", "ar").</summary>
public class Language
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;

    public ICollection<PageAttribute> PageAttributes { get; set; } = new List<PageAttribute>();
}
