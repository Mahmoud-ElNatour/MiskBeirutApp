using Microsoft.EntityFrameworkCore;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Repositories;
using MiskBeirut.Infrastructure.DbContexts;

namespace MiskBeirut.Infrastructure.Repositories;

public class ContactInquiryWhatsAppMessageRepository : Repository<ContactInquiryWhatsAppMessage>, IContactInquiryWhatsAppMessageRepository
{
    public ContactInquiryWhatsAppMessageRepository(MiskBeirutDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<ContactInquiryWhatsAppMessage>> GetByInquiryIdAsync(int contactInquiryId, CancellationToken cancellationToken = default)
        => await Context.ContactInquiryWhatsAppMessages
            .AsNoTracking()
            .Where(m => m.ContactInquiryId == contactInquiryId)
            .OrderByDescending(m => m.SentAt)
            .ToListAsync(cancellationToken);
}
