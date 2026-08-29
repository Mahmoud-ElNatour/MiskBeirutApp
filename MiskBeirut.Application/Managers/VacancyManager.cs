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

    public async Task<IReadOnlyList<VacancyDto>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var vacancies = await _vacancies.GetActiveAsync(cancellationToken);
        return vacancies.Select(ToDto).ToList();
    }

    private static VacancyDto ToDto(Vacancy vacancy) => new()
    {
        Id = vacancy.Id,
        Slug = vacancy.Slug,
        Title = vacancy.Title,
        Department = vacancy.Department,
        Location = vacancy.Location,
        EmploymentType = vacancy.EmploymentType,
        Icon = vacancy.Icon
    };
}
