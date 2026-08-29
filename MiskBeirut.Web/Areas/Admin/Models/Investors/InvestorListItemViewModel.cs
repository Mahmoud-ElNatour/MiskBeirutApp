namespace MiskBeirut.Web.Areas.Admin.Models.Investors;

public class InvestorListItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal TotalExpenses { get; set; }
    public decimal TotalWithdrawals { get; set; }
}
