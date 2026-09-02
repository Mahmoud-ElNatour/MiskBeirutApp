using System.ComponentModel.DataAnnotations;
using MiskBeirut.Application.Dtos.Careers;

namespace MiskBeirut.Web.Areas.Cms.Models.Vacancies;

/// <summary>
/// The Cms vacancy form. Lengths mirror the column limits in MiskBeirutDbContext so an over-long
/// value is refused with a field message instead of a truncation error from SQL Server.
/// </summary>
public class VacancyEditViewModel
{
    public int Id { get; set; }

    [Display(Name = "Title")]
    [Required(ErrorMessage = "A job title is required.")]
    [StringLength(200, ErrorMessage = "Keep the title under 200 characters.")]
    public string Title { get; set; } = "";

    [Display(Name = "Department")]
    [Required(ErrorMessage = "A department is required.")]
    [StringLength(100, ErrorMessage = "Keep the department under 100 characters.")]
    public string Department { get; set; } = "";

    [Display(Name = "Location")]
    [Required(ErrorMessage = "A location is required.")]
    [StringLength(100, ErrorMessage = "Keep the location under 100 characters.")]
    public string Location { get; set; } = "";

    [Display(Name = "Employment type")]
    [Required(ErrorMessage = "An employment type is required.")]
    [StringLength(50, ErrorMessage = "Keep the employment type under 50 characters.")]
    public string EmploymentType { get; set; } = "";

    [Display(Name = "Description")]
    [StringLength(4000, ErrorMessage = "Keep the description under 4000 characters.")]
    public string? Description { get; set; }

    [Display(Name = "Requirements")]
    [StringLength(4000, ErrorMessage = "Keep the requirements under 4000 characters.")]
    public string? Requirements { get; set; }

    [Display(Name = "Title (Arabic)")]
    [StringLength(200, ErrorMessage = "Keep the Arabic title under 200 characters.")]
    public string? TitleAr { get; set; }

    [Display(Name = "Department (Arabic)")]
    [StringLength(100, ErrorMessage = "Keep the Arabic department under 100 characters.")]
    public string? DepartmentAr { get; set; }

    [Display(Name = "Location (Arabic)")]
    [StringLength(100, ErrorMessage = "Keep the Arabic location under 100 characters.")]
    public string? LocationAr { get; set; }

    [Display(Name = "Employment type (Arabic)")]
    [StringLength(50, ErrorMessage = "Keep the Arabic employment type under 50 characters.")]
    public string? EmploymentTypeAr { get; set; }

    [Display(Name = "Description (Arabic)")]
    [StringLength(4000, ErrorMessage = "Keep the Arabic description under 4000 characters.")]
    public string? DescriptionAr { get; set; }

    [Display(Name = "Requirements (Arabic)")]
    [StringLength(4000, ErrorMessage = "Keep the Arabic requirements under 4000 characters.")]
    public string? RequirementsAr { get; set; }

    [Display(Name = "Application deadline")]
    [DataType(DataType.Date)]
    public DateTime? ApplicationDeadline { get; set; }

    /// <summary>A Material Symbols name (the icon set the public site already loads) — e.g. "restaurant", "local_bar".</summary>
    [Display(Name = "Icon")]
    [StringLength(50, ErrorMessage = "Keep the icon name under 50 characters.")]
    [RegularExpression("^[a-z0-9_]*$", ErrorMessage = "Icon must be a Material Symbols name — lowercase letters, digits and underscores only, e.g. restaurant.")]
    public string? Icon { get; set; }

    [Display(Name = "Status")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Display order")]
    [Range(0, 999, ErrorMessage = "Display order must be between 0 and 999.")]
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Set by the controller on an existing vacancy. Read-only: it's derived from the title on
    /// create and never re-derived afterwards, since it's the id the public Apply form toggles on
    /// and changing it would break a link someone has already been sent.
    /// </summary>
    public string? Slug { get; set; }

    public SaveVacancyRequest ToRequest() => new()
    {
        Id = Id,
        Slug = Slug,
        Title = Title,
        Department = Department,
        Location = Location,
        EmploymentType = EmploymentType,
        Description = Description,
        Requirements = Requirements,
        TitleAr = TitleAr,
        DepartmentAr = DepartmentAr,
        LocationAr = LocationAr,
        EmploymentTypeAr = EmploymentTypeAr,
        DescriptionAr = DescriptionAr,
        RequirementsAr = RequirementsAr,
        ApplicationDeadline = ApplicationDeadline,
        Icon = Icon,
        IsActive = IsActive,
        DisplayOrder = DisplayOrder
    };

    public static VacancyEditViewModel FromDto(VacancyAdminDto vacancy) => new()
    {
        Id = vacancy.Id,
        Slug = vacancy.Slug,
        Title = vacancy.Title,
        Department = vacancy.Department,
        Location = vacancy.Location,
        EmploymentType = vacancy.EmploymentType,
        Description = vacancy.Description,
        Requirements = vacancy.Requirements,
        TitleAr = vacancy.TitleAr,
        DepartmentAr = vacancy.DepartmentAr,
        LocationAr = vacancy.LocationAr,
        EmploymentTypeAr = vacancy.EmploymentTypeAr,
        DescriptionAr = vacancy.DescriptionAr,
        RequirementsAr = vacancy.RequirementsAr,
        ApplicationDeadline = vacancy.ApplicationDeadline,
        Icon = vacancy.Icon,
        IsActive = vacancy.IsActive,
        DisplayOrder = vacancy.DisplayOrder
    };
}
