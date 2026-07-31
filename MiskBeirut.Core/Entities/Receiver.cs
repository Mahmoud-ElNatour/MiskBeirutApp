namespace MiskBeirut.Core.Entities;

/// <summary>backoffice.receivers</summary>
public class Receiver
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;

    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public ICollection<InvestorTransaction> InvestorTransactions { get; set; } = new List<InvestorTransaction>();
}
