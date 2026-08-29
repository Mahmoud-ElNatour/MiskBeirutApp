using MiskBeirut.Application.Dtos.Contact;
using MiskBeirut.Core.Entities;
using MiskBeirut.Core.Repositories;

namespace MiskBeirut.Application.Managers;

/// <summary>Selectable reasons on the public Contact page's inquiry form.</summary>
public class InquiryReasonManager
{
    private readonly IInquiryReasonRepository _reasons;

    public InquiryReasonManager(IInquiryReasonRepository reasons)
    {
        _reasons = reasons;
    }

    public async Task<IReadOnlyList<InquiryReasonDto>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var reasons = await _reasons.GetActiveAsync(cancellationToken);
        return reasons.Select(ToDto).ToList();
    }

    private static InquiryReasonDto ToDto(InquiryReason reason) => new() { Id = reason.Id, Name = reason.Name };
}
