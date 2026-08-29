using MiskBeirut.Core.Enums;

namespace MiskBeirut.Application.Dtos.Customers;

/// <summary>A ledger entry plus the owning customer's name, for the cross-customer Credits/Cashbacks control-panel reports.</summary>
public sealed record CustomerLedgerReportEntryDto
{
    public int Id { get; init; }
    public DateOnly Date { get; init; }
    public decimal Amount { get; init; }
    public CustomerLedgerType Type { get; init; }
    public string? Note { get; init; }
    public int CustomerId { get; init; }
    public string CustomerName { get; init; } = null!;
}
