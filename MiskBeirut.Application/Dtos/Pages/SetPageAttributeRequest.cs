using MiskBeirut.Core.Enums;

namespace MiskBeirut.Application.Dtos.Pages;

/// <summary>
/// Upsert request: <c>PageContentManager</c> updates the existing
/// (PageId, AttributeName, LangId) row if one exists, otherwise inserts.
/// </summary>
public sealed record SetPageAttributeRequest
{
    public int PageId { get; init; }
    public string AttributeName { get; init; } = null!;
    public PageAttributeType AttributeType { get; init; } = PageAttributeType.Text;
    public int LangId { get; init; }
    public string? Value { get; init; }
}
