using MiskBeirut.Application.Dtos.Customers;
using MiskBeirut.Application.Dtos.DailyClosings;

namespace MiskBeirut.Web.Areas.Admin.Models.Customers;

public class CustomerDetailsViewModel
{
    public CustomerDto Customer { get; set; } = null!;
    public IReadOnlyList<CustomerLedgerEntryDto> Ledger { get; set; } = [];
    public IReadOnlyList<DailyClosingDto> RecentClosings { get; set; } = [];
    public AddCustomerLedgerEntryViewModel NewEntry { get; set; } = new();
}
