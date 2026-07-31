namespace MiskBeirut.Core.Entities;

/// <summary>backoffice.AuditLogs</summary>
public class AuditLog
{
    public int Id { get; set; }
    public string? EntityType { get; set; }
    public string? Action { get; set; }
    public string? EntityId { get; set; }
    public int? UserId { get; set; }
    public string? Username { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? IpAddress { get; set; }
}
