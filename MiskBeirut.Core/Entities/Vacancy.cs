namespace MiskBeirut.Core.Entities;

/// <summary>An open position shown on the public Careers page.</summary>
public class Vacancy
{
    public int Id { get; set; }
    public string Slug { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Department { get; set; } = null!;
    public string Location { get; set; } = null!;
    public string EmploymentType { get; set; } = null!;
    public string Icon { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}
