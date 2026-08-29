namespace MiskBeirut.Web.Areas.Admin.Models.DailyClosing;

// Bodies for the Daily Close page's inline "+" quick-add — deliberately separate from
// EmployeeApiRequest/CustomerApiRequest/ReceiverApiRequest (the Employees/Customers/Receivers
// pages' own add forms): those live behind their own entity-specific privilege, but adding a
// person mid-Daily-Close should only require the DailyClosing privilege already needed to be on
// this page at all — see DailyClosingController's QuickAdd* actions.

public class QuickAddEmployeeRequest
{
    public string Name { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public string? Position { get; set; }
    public decimal BaseSalary { get; set; }
}

public class QuickAddCustomerRequest
{
    public string Name { get; set; } = "";
    public string? PhoneNumber { get; set; }
}

public class QuickAddReceiverRequest
{
    public string Name { get; set; } = "";
}
