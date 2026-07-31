using MiskBeirut.Core.Entities;

namespace MiskBeirut.Core.Repositories;

public interface ILanguageRepository : IRepository<Language>
{
    Task<Language?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
}
