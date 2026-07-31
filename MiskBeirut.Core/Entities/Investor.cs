namespace MiskBeirut.Core.Entities;

/// <summary>backoffice.investors</summary>
public class Investor
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public ICollection<InvestorTransaction> Transactions { get; set; } = new List<InvestorTransaction>();
}
