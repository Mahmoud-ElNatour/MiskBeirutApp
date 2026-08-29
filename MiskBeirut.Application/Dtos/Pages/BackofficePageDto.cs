namespace MiskBeirut.Application.Dtos.Pages;

public sealed record BackofficePageDto
{
    public int Id { get; init; }
    public string PageName { get; init; } = null!;
    public IReadOnlyList<BackofficePageAttributeDto> Attributes { get; init; } = Array.Empty<BackofficePageAttributeDto>();
}
