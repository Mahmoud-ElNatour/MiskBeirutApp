using Microsoft.AspNetCore.Mvc;
using MiskBeirut.Application.Dtos.DailyClosings;
using MiskBeirut.Application.Managers;
using MiskBeirut.Core.Constants;
using MiskBeirut.Core.Enums;
using MiskBeirut.Web.Areas.Admin.Models.Home;

namespace MiskBeirut.Web.Areas.Admin.Controllers;

public class HomeController : AdminControllerBase
{
    private readonly DailyClosingManager _dailyClosings;
    private readonly CustomerManager _customers;
    private readonly EmployeeManager _employees;
    private readonly PayrollManager _payroll;
    private readonly ExpenseManager _expenses;
    private readonly InvestorManager _investors;

    public HomeController(
        BackofficePageContentManager pages,
        DailyClosingManager dailyClosings,
        CustomerManager customers,
        EmployeeManager employees,
        PayrollManager payroll,
        ExpenseManager expenses,
        InvestorManager investors) : base(pages)
    {
        _dailyClosings = dailyClosings;
        _customers = customers;
        _employees = employees;
        _payroll = payroll;
        _expenses = expenses;
        _investors = investors;
    }

    public async Task<IActionResult> Index()
    {
        var content = await LoadPageAsync("AdminHome");

        var isAdmin = User.IsInRole(RoleNames.Admin);
        var isSupervisor = User.IsInRole(RoleNames.Supervisor);

        DashboardStats? stats = null;
        DashboardCharts? charts = null;

        if (isAdmin || isSupervisor)
        {
            var now = DateTime.UtcNow;
            var closings = await _dailyClosings.GetAllAsync(now.Year, now.Month);
            var customers = await _customers.GetAllAsync();
            var payrollRecords = await _payroll.GetAllForMonthAsync(now.Year, now.Month);

            stats = new DashboardStats
            {
                ActualCashThisMonth = closings.Sum(c => c.ActualCash ?? 0),
                DailyClosingsThisMonth = closings.Count,
                OutstandingCustomerCredit = -customers.Where(c => c.Balance < 0).Sum(c => c.Balance),
                PayrollThisMonth = payrollRecords.Sum(p => p.Total ?? 0)
            };

            if (isAdmin)
            {
                var employees = await _employees.GetActiveAsync();
                var expenses = await _expenses.GetReportAsync(now.Month, now.Year, null);
                var investors = await _investors.GetActiveAsync();
                var cashbacks = await _customers.GetLedgerReportAsync(CustomerLedgerType.Cashback, now.Month, now.Year);

                stats = stats with
                {
                    ActiveEmployees = employees.Count,
                    ExpensesThisMonth = expenses.Sum(e => e.Amount),
                    TotalCustomers = customers.Count,
                    ActiveInvestors = investors.Count,
                    CustomerCashbacksThisMonth = cashbacks.Sum(c => c.Amount)
                };

                charts = await BuildChartsAsync(closings, expenses);
            }
        }

        return View(new HomeIndexViewModel
        {
            Content = content,
            IsAdmin = isAdmin,
            IsSupervisor = isSupervisor,
            Stats = stats,
            Charts = charts
        });
    }

    /// <summary>
    /// Three series: a 6-month Actual Cash trend (one extra GetAllAsync call per prior month —
    /// cheap, this page is not high-traffic), the current month's closings broken out by day
    /// (reuses the list Index already fetched), and this month's expenses folded by receiver
    /// (top 6 + "Other" — see the dataviz skill's series-count ladder for why 6 is the cap here).
    /// </summary>
    private async Task<DashboardCharts> BuildChartsAsync(IReadOnlyList<DailyClosingDto> currentMonthClosings, IReadOnlyList<ExpenseReportEntryDto> currentMonthExpenses)
    {
        var now = DateTime.UtcNow;

        var monthlyLabels = new List<string>();
        var monthlyCash = new List<decimal>();
        for (var i = 5; i >= 0; i--)
        {
            var month = now.AddMonths(-i);
            monthlyLabels.Add(month.ToString("MMM"));

            if (i == 0)
            {
                monthlyCash.Add(currentMonthClosings.Sum(c => c.ActualCash ?? 0));
            }
            else
            {
                var pastClosings = await _dailyClosings.GetAllAsync(month.Year, month.Month);
                monthlyCash.Add(pastClosings.Sum(c => c.ActualCash ?? 0));
            }
        }

        var orderedClosings = currentMonthClosings.OrderBy(c => c.Date).ToList();
        var dailyLabels = orderedClosings.Select(c => c.Date.Day.ToString()).ToList();
        var dailyCash = orderedClosings.Select(c => c.ActualCash ?? 0).ToList();

        const int maxReceivers = 6;
        var receiverTotals = currentMonthExpenses
            .GroupBy(e => e.ReceiverName)
            .Select(g => (Name: g.Key, Total: g.Sum(e => e.Amount)))
            .OrderByDescending(r => r.Total)
            .ToList();

        var receiverLabels = receiverTotals.Take(maxReceivers).Select(r => r.Name).ToList();
        var receiverAmounts = receiverTotals.Take(maxReceivers).Select(r => r.Total).ToList();
        var otherTotal = receiverTotals.Skip(maxReceivers).Sum(r => r.Total);
        if (otherTotal > 0)
        {
            receiverLabels.Add("Other");
            receiverAmounts.Add(otherTotal);
        }

        return new DashboardCharts
        {
            MonthlyLabels = monthlyLabels,
            MonthlyActualCash = monthlyCash,
            DailyLabels = dailyLabels,
            DailyActualCash = dailyCash,
            ReceiverLabels = receiverLabels,
            ReceiverAmounts = receiverAmounts
        };
    }
}
