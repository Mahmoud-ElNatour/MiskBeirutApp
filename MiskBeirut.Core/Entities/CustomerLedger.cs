using MiskBeirut.Core.Enums;

namespace MiskBeirut.Core.Entities;

/// <summary>
/// customer.customer_ledger — Credit entries are negative amounts, Cashback entries are positive.
/// References the back-office customer (backoffice.customers).
/// </summary>
public class CustomerLedger
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }
    public CustomerLedgerType Type { get; set; }
    public string? Note { get; set; }
    public int CustomerId { get; set; }
    public int DailyClosingId { get; set; }

    public Customer Customer { get; set; } = null!;
    public DailyClosing DailyClosing { get; set; } = null!;
}
