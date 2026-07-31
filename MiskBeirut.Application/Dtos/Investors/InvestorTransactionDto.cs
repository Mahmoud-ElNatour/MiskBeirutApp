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
}
