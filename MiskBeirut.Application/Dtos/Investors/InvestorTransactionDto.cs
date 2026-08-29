using MiskBeirut.Core.Enums;

namespace MiskBeirut.Application.Dtos.Investors;

public sealed record InvestorTransactionDto
{
    public int Id { get; init; }
    public DateOnly Date { get; init; }
    public decimal Amount { get; init; }
    public InvestorTransactionType TransactionType { get; init; }
    public string? Note { get; init; }
    public int DailyClosingId { get; init; }
    public int InvestorId { get; init; }
    public int? ReceiverId { get; init; }
    public string? ReceiverName { get; init; }

    /// <summary>Only populated by call sites that already load the Investor navigation (e.g. cross-investor reports); null otherwise.</summary>
    public string? InvestorName { get; init; }
}
