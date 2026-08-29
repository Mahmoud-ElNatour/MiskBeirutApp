namespace MiskBeirut.Core.Entities;

/// <summary>
/// backoffice.pages — CMS content container for the Admin-area UI (headings, blurbs, empty-state
/// copy, button labels). PageName is unique. Unlike <see cref="Page"/> (customer.pages), there is
/// no per-language split and no SEO meta: the backoffice is internal, English-only tooling.
/// </summary>
public class BackofficePage
{
    public int Id { get; set; }
    public string PageName { get; set; } = null!;

    public ICollection<BackofficePageAttribute> Attributes { get; set; } = new List<BackofficePageAttribute>();
}
