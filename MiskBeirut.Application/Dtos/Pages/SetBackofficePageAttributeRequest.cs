using MiskBeirut.Core.Enums;

namespace MiskBeirut.Application.Dtos.Pages;

/// <summary>
/// Upsert request: <c>BackofficePageContentManager</c> updates the existing
/// (PageId, AttributeName) row if one exists, otherwise inserts.
/// </summary>
public sealed record SetBackofficePageAttributeRequest
{
    public int PageId { get; init; }
    public string AttributeName { get; init; } = null!;
    public PageAttributeType AttributeType { get; init; } = PageAttributeType.Text;
    public string? Value { get; init; }
}
