using Microsoft.EntityFrameworkCore;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Repositories;
using MiskBeirut.Infrastructure.DbContexts;

namespace MiskBeirut.Infrastructure.Repositories;

public class WebsiteLeadRepository : Repository<WebsiteLead>, IWebsiteLeadRepository
{
    public WebsiteLeadRepository(MiskBeirutDbContext context) : base(context)
    {
    }

    public Task<WebsiteLead?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => Context.WebsiteLeads.FirstOrDefaultAsync(l => l.Email == email, cancellationToken);

    public Task<WebsiteLead?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
        => Context.WebsiteLeads.FirstOrDefaultAsync(l => l.PhoneNumber == phoneNumber, cancellationToken);
}
