using System.Text;
using MiskBeirut.Application.Dtos.Careers;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Repositories;

namespace MiskBeirut.Application.Managers;

/// <summary>Open positions shown on the public Careers page, and their management from the Cms.</summary>
public class VacancyManager
{
    /// <summary>Material Symbols name used when an editor doesn't pick one — the same generic icon the seeded vacancies use.</summary>
    private const string DefaultIcon = "work";

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
        var vacancies = await _vacancies.GetActiveAsync(DateTime.UtcNow, cancellationToken);
        var isArabic = string.Equals(langCode, "ar", StringComparison.OrdinalIgnoreCase);
        return vacancies.Select(v => ToDto(v, isArabic)).ToList();
    }

    /// <summary>Every vacancy including inactive and expired ones, both languages — the Cms list.</summary>
    public async Task<IReadOnlyList<VacancyAdminDto>> GetAllForAdminAsync(CancellationToken cancellationToken = default)
    {
        var vacancies = await _vacancies.GetAllOrderedAsync(cancellationToken);
        var applicationCounts = await _vacancies.GetApplicationCountsAsync(cancellationToken);
        return vacancies.Select(v => ToAdminDto(v, applicationCounts.TryGetValue(v.Id, out var count) ? count : 0)).ToList();
    }

    public async Task<VacancyAdminDto?> GetForAdminAsync(int id, CancellationToken cancellationToken = default)
    {
        var vacancy = await _vacancies.GetByIdAsync(id, cancellationToken);
        if (vacancy is null)
            return null;

        var applicationCounts = await _vacancies.GetApplicationCountsAsync(cancellationToken);
        return ToAdminDto(vacancy, applicationCounts.TryGetValue(vacancy.Id, out var count) ? count : 0);
    }

    /// <summary>
    /// Creates a vacancy (Id 0) or updates an existing one. The slug — which the public page's
    /// "Apply" toggle uses as an element id — is derived from the English title when the editor
    /// hasn't supplied one, and de-duplicated against every other vacancy.
    /// </summary>
    /// <exception cref="InvalidOperationException">The id doesn't match an existing vacancy.</exception>
    public async Task<VacancyAdminDto> SaveAsync(SaveVacancyRequest request, CancellationToken cancellationToken = default)
    {
        var isNew = request.Id == 0;
        var vacancy = isNew
            ? new Vacancy { CreatedAt = DateTime.UtcNow }
            : await _vacancies.GetByIdAsync(request.Id, cancellationToken)
              ?? throw new InvalidOperationException($"Vacancy {request.Id} was not found.");

        var desiredSlug = string.IsNullOrWhiteSpace(request.Slug) ? Slugify(request.Title) : Slugify(request.Slug);
        vacancy.Slug = await MakeSlugUniqueAsync(desiredSlug, vacancy.Id, cancellationToken);

        vacancy.Title = request.Title.Trim();
        vacancy.Department = request.Department.Trim();
        vacancy.Location = request.Location.Trim();
        vacancy.EmploymentType = request.EmploymentType.Trim();
        vacancy.Description = Trimmed(request.Description);
        vacancy.Requirements = Trimmed(request.Requirements);

        vacancy.TitleAr = Trimmed(request.TitleAr);
        vacancy.DepartmentAr = Trimmed(request.DepartmentAr);
        vacancy.LocationAr = Trimmed(request.LocationAr);
        vacancy.EmploymentTypeAr = Trimmed(request.EmploymentTypeAr);
        vacancy.DescriptionAr = Trimmed(request.DescriptionAr);
        vacancy.RequirementsAr = Trimmed(request.RequirementsAr);

        vacancy.ApplicationDeadline = request.ApplicationDeadline?.Date;
        vacancy.Icon = string.IsNullOrWhiteSpace(request.Icon) ? DefaultIcon : request.Icon.Trim();
        vacancy.IsActive = request.IsActive;
        vacancy.DisplayOrder = request.DisplayOrder;

        if (isNew)
            await _vacancies.AddAsync(vacancy, cancellationToken);
        else
            await _vacancies.UpdateAsync(vacancy, cancellationToken);

        return ToAdminDto(vacancy, 0);
    }

    public async Task SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default)
    {
        var vacancy = await _vacancies.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Vacancy {id} was not found.");

        vacancy.IsActive = isActive;
        await _vacancies.UpdateAsync(vacancy, cancellationToken);
    }

    /// <summary>
    /// Deletes a vacancy that nobody has applied to. Applications reference their vacancy (and the
    /// Cms shows the position each applicant applied for), so one with applications is refused here
    /// rather than left to surface as a foreign-key error — deactivating is what's wanted anyway.
    /// </summary>
    /// <exception cref="InvalidOperationException">The vacancy is missing, or has applications against it.</exception>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var vacancy = await _vacancies.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Vacancy {id} was not found.");

        var applicationCounts = await _vacancies.GetApplicationCountsAsync(cancellationToken);
        if (applicationCounts.TryGetValue(id, out var count) && count > 0)
            throw new InvalidOperationException(
                $"\"{vacancy.Title}\" has {count} application{(count == 1 ? "" : "s")} against it and can't be deleted without losing them. Set it to inactive instead — it disappears from the site and the applications stay.");

        await _vacancies.DeleteAsync(vacancy, cancellationToken);
    }

    private async Task<string> MakeSlugUniqueAsync(string slug, int vacancyId, CancellationToken cancellationToken)
    {
        var candidate = slug;
        var attempt = 2;
        while (await _vacancies.SlugExistsAsync(candidate, vacancyId, cancellationToken))
        {
            candidate = $"{slug}-{attempt}";
            attempt++;
        }

        return candidate;
    }

    /// <summary>
    /// Lowercase ASCII letters, digits and single hyphens — the slug ends up in an HTML element id
    /// and a querySelector on the Careers page, so anything else (including the Arabic title, if
    /// that's all an editor typed) would break the Apply toggle. Falls back to a stable placeholder
    /// which MakeSlugUniqueAsync then numbers.
    /// </summary>
    private static string Slugify(string value)
    {
        var sb = new StringBuilder();
        var lastWasHyphen = false;
        foreach (var c in value.Trim().ToLowerInvariant())
        {
            if (c is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                sb.Append(c);
                lastWasHyphen = false;
            }
            else if (!lastWasHyphen && sb.Length > 0)
            {
                sb.Append('-');
                lastWasHyphen = true;
            }
        }

        var result = sb.ToString().Trim('-');
        return result.Length == 0 ? "vacancy" : result;
    }

    private static string? Trimmed(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static VacancyDto ToDto(Vacancy vacancy, bool isArabic) => new()
    {
        Id = vacancy.Id,
        Slug = vacancy.Slug,
        Title = Localized(vacancy.TitleAr, vacancy.Title, isArabic),
        Department = Localized(vacancy.DepartmentAr, vacancy.Department, isArabic),
        Location = Localized(vacancy.LocationAr, vacancy.Location, isArabic),
        EmploymentType = Localized(vacancy.EmploymentTypeAr, vacancy.EmploymentType, isArabic),
        Description = LocalizedOrNull(vacancy.DescriptionAr, vacancy.Description, isArabic),
        Requirements = LocalizedOrNull(vacancy.RequirementsAr, vacancy.Requirements, isArabic),
        ApplicationDeadline = vacancy.ApplicationDeadline,
        Icon = vacancy.Icon,
        CreatedAt = vacancy.CreatedAt
    };

    private static VacancyAdminDto ToAdminDto(Vacancy vacancy, int applicationCount) => new()
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
        DisplayOrder = vacancy.DisplayOrder,
        CreatedAt = vacancy.CreatedAt,
        ApplicationCount = applicationCount
    };

    private static string Localized(string? arabic, string english, bool isArabic)
        => isArabic && !string.IsNullOrWhiteSpace(arabic) ? arabic! : english;

    private static string? LocalizedOrNull(string? arabic, string? english, bool isArabic)
        => isArabic && !string.IsNullOrWhiteSpace(arabic) ? arabic : english;
}
