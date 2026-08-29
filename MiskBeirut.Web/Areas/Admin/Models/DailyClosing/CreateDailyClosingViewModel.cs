using System.ComponentModel.DataAnnotations;
using MiskBeirut.Application.Dtos.Customers;
using MiskBeirut.Application.Dtos.Employees;
using MiskBeirut.Application.Dtos.Investors;
using MiskBeirut.Application.Dtos.Receivers;

namespace MiskBeirut.Web.Areas.Admin.Models.DailyClosing;

public class CreateDailyClosingViewModel
{
    [Required]
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);

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

    /// <summary>Dropdown sources for the row templates — populated by the GET action, not posted back.</summary>
    public IReadOnlyList<ReceiverDto> Receivers { get; set; } = [];
    public IReadOnlyList<InvestorDto> Investors { get; set; } = [];
    public IReadOnlyList<EmployeeDto> Employees { get; set; } = [];
    public IReadOnlyList<CustomerDto> Customers { get; set; } = [];
}

public class GeneralExpenseRowViewModel
{
    // [Required] alone doesn't reject 0 here — a non-nullable value type is never "missing" as far
    // as RequiredAttribute is concerned, so an unpicked select (which posts 0) would sail past
    // validation and hit the ReceiverId foreign key at the database. Range makes 0 actually invalid.
    [Range(1, int.MaxValue, ErrorMessage = "Pick a receiver for every general expense row.")]
    public int ReceiverId { get; set; }

    [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "Every general expense row needs an amount greater than 0.")]
    public decimal Amount { get; set; }

    public string? Note { get; set; }
}

public class InvestorExpenseRowViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Pick an investor for every investor expense row.")]
    public int InvestorId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Pick a receiver for every investor expense row.")]
    public int ReceiverId { get; set; }

    [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "Every investor expense row needs an amount greater than 0.")]
    public decimal Amount { get; set; }

    public string? Note { get; set; }
}

/// <summary>Shared row shape for both Advances and Deductions — Amount is always entered positive;
/// which list it's posted under decides the ledger type.</summary>
public class EmployeeLedgerRowViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Pick an employee for every advance/deduction row.")]
    public int EmployeeId { get; set; }

    [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "Every advance/deduction row needs an amount greater than 0.")]
    public decimal Amount { get; set; }

    public string? Note { get; set; }
}

/// <summary>Shared row shape for both Credits and Cashbacks — Amount is always entered positive;
/// which list it's posted under decides the ledger type.</summary>
public class CustomerLedgerRowViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Pick a customer for every credit/cashback row.")]
    public int CustomerId { get; set; }

    [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "Every credit/cashback row needs an amount greater than 0.")]
    public decimal Amount { get; set; }

    public string? Note { get; set; }
}

public class NonCashPaymentRowViewModel
{
    [Required(ErrorMessage = "Pick or type a payment method for every non-cash payment row.")]
    public string PaymentMethod { get; set; } = "";

    [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "Every non-cash payment row needs an amount greater than 0.")]
    public decimal Amount { get; set; }

    /// <summary>Bill reference is captured here as free text — there's no separate BillRef column.</summary>
    public string? Note { get; set; }
}
