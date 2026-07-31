using MiskBeirut.Core.Enums;

namespace MiskBeirut.Application.Dtos.Pages;

public sealed record PageAttributeDto
{
    public int Id { get; init; }
    public int PageId { get; init; }
    public string AttributeName { get; init; } = null!;
    public PageAttributeType AttributeType { get; init; }
    public int LangId { get; init; }
    public string? Value { get; init; }
}
