using Microsoft.EntityFrameworkCore;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Repositories;
using MiskBeirut.Infrastructure.DbContexts;

namespace MiskBeirut.Infrastructure.Repositories;

public class LanguageRepository : Repository<Language>, ILanguageRepository
{
    public LanguageRepository(MiskBeirutDbContext context) : base(context)
    {
    }

    public Task<Language?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        => Context.Languages.FirstOrDefaultAsync(l => l.Code == code, cancellationToken);
}
