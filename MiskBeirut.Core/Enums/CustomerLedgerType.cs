namespace MiskBeirut.Core.Enums;

/// <summary>
/// Stored as a string in customer.customer_ledger.Type.
/// Credit entries must have a negative Amount; Cashback entries must have a positive Amount.
/// </summary>
public enum CustomerLedgerType
{
    Credit,
    Cashback
}
