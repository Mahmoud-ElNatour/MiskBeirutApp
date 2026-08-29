namespace MiskBeirut.Application.Dtos.Privileges;

public sealed record PrivilegeDto
{
    public int Id { get; init; }
    public string Key { get; init; } = null!;
    public string Name { get; init; } = null!;
    public bool IsSection { get; init; }
    public string? SectionKey { get; init; }
}
