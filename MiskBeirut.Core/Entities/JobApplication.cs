namespace MiskBeirut.Core.Entities;

/// <summary>A candidate's application to an open <see cref="Vacancy"/>, submitted via the public Careers page.</summary>
public class JobApplication
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Address { get; set; }
    public string CvUrl { get; set; } = null!;
    public int VacancyId { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Whether HR has made a hire/reject decision on this application yet — a simple
    /// yes/no flag, not a multi-stage pipeline status.</summary>
    public bool DecisionTaken { get; set; }

    public Vacancy Vacancy { get; set; } = null!;
}
