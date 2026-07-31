namespace MiskBeirut.Application.Dtos.Pages;

public sealed record LanguageDto
{
    public int Id { get; init; }
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
}
