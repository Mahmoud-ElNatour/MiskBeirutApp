namespace MiskBeirut.Application.Dtos.Pages;

public sealed record PageDto
{
    public int Id { get; init; }
    public string PageName { get; init; } = null!;
    public string? MetaTitle { get; init; }
    public string? MetaDesc { get; init; }
    public string? MetaKeyword { get; init; }
    public IReadOnlyList<PageAttributeDto> Attributes { get; init; } = Array.Empty<PageAttributeDto>();
}
