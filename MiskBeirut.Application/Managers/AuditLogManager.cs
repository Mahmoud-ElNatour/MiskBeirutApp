using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MiskBeirut.Application.Dtos.Audit;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Repositories;

namespace MiskBeirut.Application.Managers;

/// <summary>Records who changed what. Writing a log entry is best-effort — it must never block the operation being logged.</summary>
public class AuditLogManager
{
    private readonly IAuditLogRepository _auditLogs;
    private readonly ILogger<AuditLogManager> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditLogManager(IAuditLogRepository auditLogs, ILogger<AuditLogManager> logger, IHttpContextAccessor httpContextAccessor)
    {
        _auditLogs = auditLogs;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IReadOnlyList<AuditLogDto>> GetRecentAsync(int count = 200, CancellationToken cancellationToken = default)
    {
        var logs = await _auditLogs.GetRecentAsync(count, cancellationToken);
        return logs.Select(ToDto).ToList();
    }

    /// <summary>Every log entry for a given month/year — used by the Audit Logs page's month filter. Falls back to the most recent 300 when neither is set, matching the page's previous unfiltered view.</summary>
    public async Task<IReadOnlyList<AuditLogDto>> GetAsync(int? month, int? year, CancellationToken cancellationToken = default)
    {
        var logs = month.HasValue || year.HasValue
            ? await _auditLogs.GetByMonthAsync(month, year, cancellationToken)
            : await _auditLogs.GetRecentAsync(300, cancellationToken);
        return logs.Select(ToDto).ToList();
    }

    public async Task<AuditLogDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var log = await _auditLogs.GetByIdAsync(id, cancellationToken);
        return log is null ? null : ToDto(log);
    }

    public async Task LogAsync(
        string entityType,
        string action,
        string? entityId,
        int? userId,
        string? username,
        string? description = null,
        string? oldValues = null,
        string? newValues = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _auditLogs.AddAsync(new AuditLog
            {
                EntityType = entityType,
                Action = action,
                EntityId = entityId,
                UserId = userId,
                Username = username,
                OldValues = oldValues,
                NewValues = newValues,
                Description = description,
                CreatedAt = DateTime.UtcNow,
                IpAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write audit log for {EntityType}/{Action}/{EntityId}", entityType, action, entityId);
        }
    }

    private static AuditLogDto ToDto(AuditLog log) => new()
    {
        Id = log.Id,
        EntityType = log.EntityType,
        Action = log.Action,
        EntityId = log.EntityId,
        UserId = log.UserId,
        Username = log.Username,
        OldValues = log.OldValues,
        NewValues = log.NewValues,
        Description = log.Description,
        CreatedAt = log.CreatedAt,
        IpAddress = log.IpAddress
    };
}
