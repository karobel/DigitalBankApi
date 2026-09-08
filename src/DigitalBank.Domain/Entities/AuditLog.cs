namespace DigitalBank.Domain.Entities;

/// <summary>
/// Immutable audit trail entry. Banking systems must log who did what, when —
/// this mirrors the audit/security requirements seen in real banking environments.
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? Details { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
