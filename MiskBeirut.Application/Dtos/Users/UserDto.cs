namespace MiskBeirut.Application.Dtos.Users;

/// <summary>Never exposes the password hash or security stamps.</summary>
public sealed record UserDto
{
    public int Id { get; init; }
    public string Username { get; init; } = null!;
    public string? Email { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = [];
    public string? PhoneNumber { get; init; }
    public DateTime CreatedAt { get; init; }
}
