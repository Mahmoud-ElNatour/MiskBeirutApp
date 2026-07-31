using MiskBeirut.Core.Enums;

namespace MiskBeirut.Core.Entities;

/// <summary>
/// customer.page_attributes — unique per (PageId, AttributeName, LangId).
/// </summary>
public class PageAttribute
{
    public int Id { get; set; }
    public int PageId { get; set; }
    public string AttributeName { get; set; } = null!;
    public PageAttributeType AttributeType { get; set; } = PageAttributeType.Text;
    public int LangId { get; set; }
    public string? Value { get; set; }

    public Page Page { get; set; } = null!;
    public Language Language { get; set; } = null!;
}
