using MiskBeirut.Application.Dtos.DailyClosings;
using MiskBeirut.Application.Dtos.Investors;
using MiskBeirut.Application.Dtos.Receivers;

namespace MiskBeirut.Web.Areas.Admin.Models.Investors;

public class InvestorDetailsViewModel
{
    public InvestorDto Investor { get; set; } = null!;
    public IReadOnlyList<InvestorTransactionDto> Expenses { get; set; } = [];
    public IReadOnlyList<InvestorTransactionDto> Withdrawals { get; set; } = [];

    /// <summary>Expense totals grouped by receiver — "every investor has expenses and receivers."</summary>
    public IReadOnlyList<InvestorReceiverBreakdownItem> ReceiverBreakdown { get; set; } = [];

    public IReadOnlyList<ReceiverDto> Receivers { get; set; } = [];
    public IReadOnlyList<DailyClosingDto> RecentClosings { get; set; } = [];
    public AddInvestorTransactionViewModel NewTransaction { get; set; } = new();
}

public class InvestorReceiverBreakdownItem
{
    public int ReceiverId { get; set; }
    public string ReceiverName { get; set; } = "";
    public decimal Total { get; set; }
}
