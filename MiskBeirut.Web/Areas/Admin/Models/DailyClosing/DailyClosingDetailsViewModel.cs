using MiskBeirut.Application.Dtos.DailyClosings;
using MiskBeirut.Application.Dtos.Receivers;

namespace MiskBeirut.Web.Areas.Admin.Models.DailyClosing;

public class DailyClosingDetailsViewModel
{
    public DailyClosingDto Closing { get; set; } = null!;
    public DailyClosingSummaryDto? Summary { get; set; }
    public IReadOnlyList<ExpenseDto> Expenses { get; set; } = [];
    public IReadOnlyList<NonCashPaymentDto> NonCashPayments { get; set; } = [];
    public IReadOnlyList<ReceiverDto> Receivers { get; set; } = [];
    public AddExpenseViewModel NewExpense { get; set; } = new();
    public AddNonCashPaymentViewModel NewNonCashPayment { get; set; } = new();

    /// <summary>Employee is only ever allowed onto this page for today's own entry (enforced server-side too) —
    /// this just controls whether Edit/Delete/navigation-away affordances render.</summary>
    public bool CanManageHeader { get; set; }
}
