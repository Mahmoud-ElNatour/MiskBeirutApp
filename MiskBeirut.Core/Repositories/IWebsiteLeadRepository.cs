using MiskBeirut.Core.Entities;

namespace MiskBeirut.Core.Repositories;

/// <summary>Leads captured by the website popup (customer.customers).</summary>
public interface IWebsiteLeadRepository : IRepository<WebsiteLead>
{
    Task<WebsiteLead?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<WebsiteLead?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default);
}
