using MiskBeirut.Core.Enums;

namespace MiskBeirut.Core.Entities;

/// <summary>
/// backoffice.pageattributes — unique per (PageId, AttributeName). Reuses the same
/// <see cref="PageAttributeType"/> vocabulary as the customer-side CMS attributes.
/// </summary>
public class BackofficePageAttribute
{
    public int Id { get; set; }
    public int PageId { get; set; }
    public string AttributeName { get; set; } = null!;
    public PageAttributeType AttributeType { get; set; } = PageAttributeType.Text;
    public string? Value { get; set; }

    public BackofficePage Page { get; set; } = null!;
}
