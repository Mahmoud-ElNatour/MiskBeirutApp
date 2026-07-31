namespace MiskBeirut.Core.Entities;

/// <summary>backoffice.employees</summary>
public class Employee
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string? Position { get; set; }
    public decimal BaseSalary { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UserId { get; set; }

    public User? User { get; set; }
    public ICollection<EmployeeWorking> WorkingRecords { get; set; } = new List<EmployeeWorking>();
    public ICollection<EmployeeLedger> LedgerEntries { get; set; } = new List<EmployeeLedger>();
}
