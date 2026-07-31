namespace MiskBeirut.Application.Dtos.DailyClosings;

/// <summary>
/// Aggregated totals for one daily closing. Employee ledger amounts are stored negative;
/// only Advance entries are included in <see cref="TotalEmployeeAdvances"/> because
/// Deduct entries do not affect cash-drawer reconciliation.
/// </summary>
public sealed record DailyClosingSummaryDto
{
    public int DailyClosingId { get; init; }
    public DateOnly Date { get; init; }
    public decimal MainReading { get; init; }
    public decimal? AdjustedReading { get; init; }
    public decimal? ActualCash { get; init; }
    public decimal TotalExpenses { get; init; }
    public decimal TotalNonCashPayments { get; init; }
    public decimal TotalEmployeeAdvances { get; init; }
    public decimal TotalCustomerCredits { get; init; }
    public decimal TotalCustomerCashbacks { get; init; }
    public decimal TotalInvestorWithdrawals { get; init; }
    public decimal TotalInvestorExpenses { get; init; }
}
