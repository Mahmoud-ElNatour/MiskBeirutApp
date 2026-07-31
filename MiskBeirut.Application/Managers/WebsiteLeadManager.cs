using MiskBeirut.Application.Dtos.Leads;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Repositories;

namespace MiskBeirut.Application.Managers;

/// <summary>Leads captured by the website's discount popup.</summary>
public class WebsiteLeadManager
{
    private readonly IWebsiteLeadRepository _leads;

    public WebsiteLeadManager(IWebsiteLeadRepository leads)
    {
        _leads = leads;
    }

    public async Task<(WebsiteLeadDto Lead, bool AlreadyClaimed)> CreateAsync(string name, string phoneNumber, string email, CancellationToken cancellationToken = default)
    {
        var existing = await _leads.GetByEmailAsync(email, cancellationToken)
            ?? await _leads.GetByPhoneNumberAsync(phoneNumber, cancellationToken);
        if (existing is not null)
            return (ToDto(existing), true);

        var lead = await _leads.AddAsync(new WebsiteLead
        {
            Name = name,
            PhoneNumber = phoneNumber,
            Email = email
        }, cancellationToken);

        return (ToDto(lead), false);
    }

    private static WebsiteLeadDto ToDto(WebsiteLead lead) => new()
    {
        Id = lead.Id,
        Name = lead.Name,
        PhoneNumber = lead.PhoneNumber,
        Email = lead.Email,
        DiscountRedeemed = lead.DiscountRedeemed,
        CreatedAt = lead.CreatedAt
    };
}
