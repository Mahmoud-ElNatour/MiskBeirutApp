using MiskBeirut.Core.Enums;

namespace MiskBeirut.Application.Dtos.Investors;

/// <summary>
/// Expense transactions require a ReceiverId; Withdrawal transactions do not —
/// enforced by <c>InvestorManager</c> before the row is written.
/// </summary>
public sealed record CreateInvestorTransactionRequest
{
    public DateOnly Date { get; init; }
    public decimal Amount { get; init; }
    public InvestorTransactionType TransactionType { get; init; }
    public string? Note { get; init; }
    public int DailyClosingId { get; init; }
    public int InvestorId { get; init; }
    public int? ReceiverId { get; init; }
}
