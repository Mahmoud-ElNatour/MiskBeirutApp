namespace MiskBeirut.Core.Entities;

/// <summary>backoffice.expenses</summary>
public class Expense
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }

    /// <summary>
    /// Null for a manual expense added before that date's Daily Closing exists — see
    /// <see cref="IsManualEntry"/>. <see cref="Managers.DailyClosingManager"/> attaches any
    /// same-date unattached expense to a closing when one is created or edited.
    /// </summary>
    public int? DailyClosingId { get; set; }
    public int ReceiverId { get; set; }

    /// <summary>
    /// True when this expense was added standalone (Control Panel Expenses page's "Add Expense"
    /// form) rather than as one of a Daily Closing's submitted line items. Manual expenses are left
    /// alone — not deleted/re-added — when their Daily Closing is later edited via
    /// <see cref="Managers.DailyClosingManager.UpdateWithLinesAsync"/>.
    /// </summary>
    public bool IsManualEntry { get; set; }

    public DailyClosing? DailyClosing { get; set; }
    public Receiver Receiver { get; set; } = null!;
}
