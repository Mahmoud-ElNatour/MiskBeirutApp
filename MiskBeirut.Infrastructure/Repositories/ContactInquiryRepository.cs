using Microsoft.EntityFrameworkCore;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Repositories;
using MiskBeirut.Infrastructure.DbContexts;

namespace MiskBeirut.Infrastructure.Repositories;

public class ContactInquiryRepository : Repository<ContactInquiry>, IContactInquiryRepository
{
    public ContactInquiryRepository(MiskBeirutDbContext context) : base(context)
    {
    }

    /// <summary>Overridden to eager-load Reason — the Cms listing shows the reason name per inquiry.</summary>
    public override async Task<IReadOnlyList<ContactInquiry>> GetAllAsync(CancellationToken cancellationToken = default)
        => await Context.ContactInquiries
            .AsNoTracking()
            .Include(i => i.Reason)
            .ToListAsync(cancellationToken);
}
