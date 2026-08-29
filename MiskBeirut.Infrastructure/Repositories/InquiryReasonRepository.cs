using Microsoft.EntityFrameworkCore;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Repositories;
using MiskBeirut.Infrastructure.DbContexts;

namespace MiskBeirut.Infrastructure.Repositories;

public class InquiryReasonRepository : Repository<InquiryReason>, IInquiryReasonRepository
{
    public InquiryReasonRepository(MiskBeirutDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<InquiryReason>> GetActiveAsync(CancellationToken cancellationToken = default)
        => await Context.InquiryReasons
            .AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.DisplayOrder)
            .ThenBy(r => r.Id)
            .ToListAsync(cancellationToken);
}
