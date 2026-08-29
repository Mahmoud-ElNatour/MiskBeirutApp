namespace MiskBeirut.Web.Areas.Admin.Models.Home;

public class HomeIndexViewModel
{
    public required MiskBeirut.Web.Support.BackofficePageContent Content { get; init; }
    public bool IsAdmin { get; init; }
    public bool IsSupervisor { get; init; }

    /// <summary>Null for roles with no daily-operations privileges (e.g. a plain Employee) — the dashboard falls back to just the welcome cards.</summary>
    public DashboardStats? Stats { get; init; }

    /// <summary>Admin-only — Supervisor's role doesn't cover Employees/Expenses/Investors, the areas these charts break down.</summary>
    public DashboardCharts? Charts { get; init; }
}

/// <summary>
/// KPI figures for the dashboard's stat-card row. The four "shared" figures are computed for
/// anyone who can see stats at all (Admin or Supervisor — matches their common ground: Daily
/// Closing + Customer access). The rest are Admin-only, whose role additionally covers Employees,
/// Expenses, and Investors — Supervisor's own role is Payroll *read* only, which the shared
/// PayrollThisMonth figure already covers read-only in the UI.
/// </summary>
public record DashboardStats
{
    public decimal ActualCashThisMonth { get; init; }
    public int DailyClosingsThisMonth { get; init; }
    public decimal OutstandingCustomerCredit { get; init; }
    public decimal PayrollThisMonth { get; init; }

    public int? ActiveEmployees { get; init; }
    public decimal? ExpensesThisMonth { get; init; }
    public int? TotalCustomers { get; init; }
    public int? ActiveInvestors { get; init; }
    public decimal? CustomerCashbacksThisMonth { get; init; }
}

/// <summary>Chart series for the Admin dashboard — plain label/value arrays, serialized straight to JSON for Chart.js.</summary>
public record DashboardCharts
{
    public required IReadOnlyList<string> MonthlyLabels { get; init; }
    public required IReadOnlyList<decimal> MonthlyActualCash { get; init; }

    public required IReadOnlyList<string> DailyLabels { get; init; }
    public required IReadOnlyList<decimal> DailyActualCash { get; init; }

    public required IReadOnlyList<string> ReceiverLabels { get; init; }
    public required IReadOnlyList<decimal> ReceiverAmounts { get; init; }
}
