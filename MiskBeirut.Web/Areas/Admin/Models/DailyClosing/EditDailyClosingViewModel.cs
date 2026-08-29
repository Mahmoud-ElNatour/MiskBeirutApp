using System.ComponentModel.DataAnnotations;
using MiskBeirut.Application.Dtos.Customers;
using MiskBeirut.Application.Dtos.Employees;
using MiskBeirut.Application.Dtos.Investors;
using MiskBeirut.Application.Dtos.Receivers;

namespace MiskBeirut.Web.Areas.Admin.Models.DailyClosing;

/// <summary>
/// The Edit page mirrors Create exactly — same locked/unlockable Date, same repeatable sections —
/// just pre-filled from the closing's current header and line items. Row shapes are shared with
/// <see cref="CreateDailyClosingViewModel"/>; saving replaces every line item wholesale rather than
/// diffing row-by-row (see <c>DailyClosingManager.UpdateWithLinesAsync</c>).
/// </summary>
public class EditDailyClosingViewModel
{
    public int Id { get; set; }

    [Required]
    public DateOnly Date { get; set; }

    [Required]
    public decimal MainReading { get; set; }

    public string? Note { get; set; }

    public List<GeneralExpenseRowViewModel> GeneralExpenses { get; set; } = [];
    public List<InvestorExpenseRowViewModel> InvestorExpenses { get; set; } = [];
    public List<EmployeeLedgerRowViewModel> Advances { get; set; } = [];
    public List<EmployeeLedgerRowViewModel> Deductions { get; set; } = [];
    public List<CustomerLedgerRowViewModel> Credits { get; set; } = [];
    public List<CustomerLedgerRowViewModel> Cashbacks { get; set; } = [];
    public List<NonCashPaymentRowViewModel> NonCashPayments { get; set; } = [];

    /// <summary>Dropdown sources for the row templates — populated by the controller, not posted back.</summary>
    public IReadOnlyList<ReceiverDto> Receivers { get; set; } = [];
    public IReadOnlyList<InvestorDto> Investors { get; set; } = [];
    public IReadOnlyList<EmployeeDto> Employees { get; set; } = [];
    public IReadOnlyList<CustomerDto> Customers { get; set; } = [];
}
