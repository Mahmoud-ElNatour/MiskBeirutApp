using MiskBeirut.Application.Dtos.DailyClosings;
using MiskBeirut.Application.Dtos.Receivers;

namespace MiskBeirut.Web.Areas.Admin.Models.DailyClosing;

/// <summary>A read-only, print-oriented summary of one closing — no inline editing here (Edit is
/// reached from the Index list instead).</summary>
public class DailyClosingDetailsViewModel
{
    public DailyClosingDto Closing { get; set; } = null!;
    public DailyClosingSummaryDto? Summary { get; set; }
    public IReadOnlyList<ExpenseDto> Expenses { get; set; } = [];
    public IReadOnlyList<NonCashPaymentDto> NonCashPayments { get; set; } = [];
    public IReadOnlyList<ReceiverDto> Receivers { get; set; } = [];
    public DailyClosingBreakdownDto Breakdown { get; set; } = new();
}
