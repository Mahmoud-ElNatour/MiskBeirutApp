namespace MiskBeirut.Core.Entities;

/// <summary>backoffice.daily_closing — one row per business day (Date is unique).</summary>
public class DailyClosing
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public decimal MainReading { get; set; }
    public decimal? AdjustedReading { get; set; }
    public decimal? ActualCash { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public ICollection<NonCashPayment> NonCashPayments { get; set; } = new List<NonCashPayment>();
    public ICollection<EmployeeLedger> EmployeeLedgerEntries { get; set; } = new List<EmployeeLedger>();
    public ICollection<InvestorTransaction> InvestorTransactions { get; set; } = new List<InvestorTransaction>();
    public ICollection<CustomerLedger> CustomerLedgerEntries { get; set; } = new List<CustomerLedger>();
}
