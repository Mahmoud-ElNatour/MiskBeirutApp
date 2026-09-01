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

    /// <param name="langCode">
    /// Site language the visitor is browsing in. "ar" returns each reason's Arabic label, falling
    /// back to the English one where no translation has been entered.
    /// </param>
    public async Task<IReadOnlyList<InquiryReasonDto>> GetActiveAsync(string langCode = "en", CancellationToken cancellationToken = default)
    {
        var reasons = await _reasons.GetActiveAsync(cancellationToken);
        var isArabic = string.Equals(langCode, "ar", StringComparison.OrdinalIgnoreCase);
        return reasons.Select(r => ToDto(r, isArabic)).ToList();
    }

    private static InquiryReasonDto ToDto(InquiryReason reason, bool isArabic) => new()
    {
        Id = reason.Id,
        Name = isArabic && !string.IsNullOrWhiteSpace(reason.NameAr) ? reason.NameAr! : reason.Name
    };
}
