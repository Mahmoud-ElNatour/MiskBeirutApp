using MiskBeirut.Application.Dtos.Customers;
using MiskBeirut.Application.Dtos.Employees;
using MiskBeirut.Application.Dtos.Investors;

namespace MiskBeirut.Application.Dtos.DailyClosings;

/// <summary>
/// The named ledger movements for one daily closing — the detail sections on the Daily Close
/// print/details page. Investor expenses replace the old app's hardcoded "Ahmad"/"Samer" expense
/// categories; any number of investors can appear here, each identified by name.
/// </summary>
public sealed record DailyClosingBreakdownDto
{
    public IReadOnlyList<EmployeeLedgerReportEntryDto> Advances { get; init; } = [];
    public IReadOnlyList<EmployeeLedgerReportEntryDto> Deductions { get; init; } = [];
    public IReadOnlyList<CustomerLedgerReportEntryDto> Credits { get; init; } = [];
    public IReadOnlyList<CustomerLedgerReportEntryDto> Cashbacks { get; init; } = [];
    public IReadOnlyList<InvestorTransactionDto> InvestorExpenses { get; init; } = [];
}