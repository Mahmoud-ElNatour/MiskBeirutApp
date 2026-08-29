using MiskBeirut.Core.Entities;

namespace MiskBeirut.Core.Repositories;

public interface IInquiryReasonRepository : IRepository<InquiryReason>
{
    Task<IReadOnlyList<InquiryReason>> GetActiveAsync(CancellationToken cancellationToken = default);
}
