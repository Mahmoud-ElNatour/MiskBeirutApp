using MiskBeirut.Core.Enums;

namespace MiskBeirut.Application.Dtos.Customers;

/// <summary>
/// Credit entries must carry a negative Amount, Cashback entries a positive Amount —
/// enforced by <c>CustomerManager</c> before the row is written.
/// </summary>
public sealed record CreateCustomerLedgerEntryRequest
{
    public DateOnly Date { get; init; }
    public decimal Amount { get; init; }
    public CustomerLedgerType Type { get; init; }
    public string? Note { get; init; }
    public int CustomerId { get; init; }
    public int DailyClosingId { get; init; }
}
