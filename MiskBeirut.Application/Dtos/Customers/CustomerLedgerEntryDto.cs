using MiskBeirut.Core.Enums;

namespace MiskBeirut.Application.Dtos.Customers;

public sealed record CustomerLedgerEntryDto
{
    public int Id { get; init; }
    public DateOnly Date { get; init; }
    public decimal Amount { get; init; }
    public CustomerLedgerType Type { get; init; }
    public string? Note { get; init; }
    public int CustomerId { get; init; }
    public int? DailyClosingId { get; init; }
    public bool IsManualEntry { get; init; }
}
