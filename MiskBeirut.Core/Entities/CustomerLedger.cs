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

    /// <summary>
    /// Null for a manual entry added before that date's Daily Closing exists — see
    /// <see cref="IsManualEntry"/>. <see cref="Managers.DailyClosingManager"/> attaches any
    /// same-date unattached entry to a closing when one is created or edited.
    /// </summary>
    public int? DailyClosingId { get; set; }

    /// <summary>
    /// True when this entry was added standalone (Customer Details "Add Entry" form or a balance
    /// edit made with no closing yet for that date) rather than as one of a Daily Closing's
    /// submitted line items. Manual entries are left alone — not deleted/re-added — when their
    /// Daily Closing is later edited via <see cref="Managers.DailyClosingManager.UpdateWithLinesAsync"/>.
    /// </summary>
    public bool IsManualEntry { get; set; }

    public Customer Customer { get; set; } = null!;
    public DailyClosing? DailyClosing { get; set; }
}
