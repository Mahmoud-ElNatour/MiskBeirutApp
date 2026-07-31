using MiskBeirut.Core.Enums;

namespace MiskBeirut.Core.Entities;

/// <summary>
/// backoffice.investor_transactions — Expense transactions require a ReceiverId.
/// </summary>
public class InvestorTransaction
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }
    public InvestorTransactionType TransactionType { get; set; }
    public string? Note { get; set; }
    public int DailyClosingId { get; set; }
    public int InvestorId { get; set; }
    public int? ReceiverId { get; set; }

    public DailyClosing DailyClosing { get; set; } = null!;
    public Investor Investor { get; set; } = null!;
    public Receiver? Receiver { get; set; }
}
