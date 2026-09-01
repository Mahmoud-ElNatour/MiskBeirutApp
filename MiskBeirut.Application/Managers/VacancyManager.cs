using MiskBeirut.Application.Dtos.Careers;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Repositories;

namespace MiskBeirut.Application.Managers;

/// <summary>Open positions shown on the public Careers page.</summary>
public class VacancyManager
{
    private readonly IVacancyRepository _vacancies;

    public VacancyManager(IVacancyRepository vacancies)
    {
        _vacancies = vacancies;
    }

    /// <param name="langCode">
    /// Site language the visitor is browsing in. "ar" returns each vacancy's Arabic copy, falling
    /// back per-field to the English one where no translation has been entered — so a newly added
    /// vacancy still renders on the Arabic Careers page instead of showing blanks.
    /// </param>
    public async Task<IReadOnlyList<VacancyDto>> GetActiveAsync(string langCode = "en", CancellationToken cancellationToken = default)
    {
        var vacancies = await _vacancies.GetActiveAsync(cancellationToken);
        var isArabic = string.Equals(langCode, "ar", StringComparison.OrdinalIgnoreCase);
        return vacancies.Select(v => ToDto(v, isArabic)).ToList();
    }

    private static VacancyDto ToDto(Vacancy vacancy, bool isArabic) => new()
    {
        Id = vacancy.Id,
        Slug = vacancy.Slug,
        Title = Localized(vacancy.TitleAr, vacancy.Title, isArabic),
        Department = Localized(vacancy.DepartmentAr, vacancy.Department, isArabic),
        Location = Localized(vacancy.LocationAr, vacancy.Location, isArabic),
        EmploymentType = Localized(vacancy.EmploymentTypeAr, vacancy.EmploymentType, isArabic),
        Icon = vacancy.Icon
    };

    private static string Localized(string? arabic, string english, bool isArabic)
        => isArabic && !string.IsNullOrWhiteSpace(arabic) ? arabic! : english;
}
