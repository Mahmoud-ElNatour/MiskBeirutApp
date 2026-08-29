using MiskBeirut.Core.Entities;

namespace MiskBeirut.Core.Repositories;

public interface IContactInquiryWhatsAppMessageRepository : IRepository<ContactInquiryWhatsAppMessage>
{
    /// <summary>Send history for one inquiry, most recent first.</summary>
    Task<IReadOnlyList<ContactInquiryWhatsAppMessage>> GetByInquiryIdAsync(int contactInquiryId, CancellationToken cancellationToken = default);
}
