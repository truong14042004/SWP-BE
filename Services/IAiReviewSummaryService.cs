using SWP_BE.Models;

namespace SWP_BE.Services;

public interface IAiReviewSummaryService
{
    /// <summary>
    /// Quet evidence (hien tai chi ho tro GitHub repo) cua mot review request
    /// va sinh ra AiReviewSummary. Neu da co summary ton tai, xoa cai cu va tao lai.
    /// </summary>
    Task<AiReviewSummary> GenerateAsync(
        Guid reviewRequestId,
        Guid generatedByUserId,
        CancellationToken cancellationToken);
}
