namespace MiskBeirut.Application.Dtos.Audit;

public sealed record AuditLogDto
{
    public int Id { get; init; }
    public string? EntityType { get; init; }
    public string? Action { get; init; }
    public string? EntityId { get; init; }
    public int? UserId { get; init; }
    public string? Username { get; init; }
    public string? OldValues { get; init; }
    public string? NewValues { get; init; }
    public string? Description { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? IpAddress { get; init; }
}
