using DigitalBank.Domain.Enums;

namespace DigitalBank.Domain.Entities;

/// <summary>
/// Represents a single financial movement (deposit, withdrawal, or transfer leg) on an account.
/// </summary>
public class Transaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AccountId { get; set; }
    public Account? Account { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? Description { get; set; }
    public Guid? RelatedTransferId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
