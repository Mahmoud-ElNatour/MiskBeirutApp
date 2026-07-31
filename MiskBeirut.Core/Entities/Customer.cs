namespace MiskBeirut.Core.Entities;

/// <summary>backoffice.customers</summary>
public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public decimal Balance { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<CustomerLedger> LedgerEntries { get; set; } = new List<CustomerLedger>();
}
