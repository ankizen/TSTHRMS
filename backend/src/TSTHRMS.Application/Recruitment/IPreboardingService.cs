using TSTHRMS.Application.Recruitment.Dtos;
using TSTHRMS.Domain.Recruitment;

namespace TSTHRMS.Application.Recruitment;

/// <summary>
/// Section 10: the checklist auto-created the moment an offer is accepted. Document tasks map to
/// the same categories Core HR uses (Education/Identity/Previous Employment) so Slice 8's Day-1
/// conversion can carry them across without asking the new hire to submit anything twice.
/// </summary>
public interface IPreboardingService
{
    /// <summary>Idempotent - a second call for the same application is a no-op. Called by
    /// IOfferService right after Stage flips to OfferAccepted, not exposed as its own endpoint.</summary>
    Task CreateChecklistAsync(Guid applicationId, CancellationToken cancellationToken = default);

    /// <summary>Null means not found/no access (same ownership scoping as the rest of the
    /// internal recruitment surface).</summary>
    Task<IReadOnlyList<PreboardingChecklistItemDto>?> GetChecklistAsync(
        Guid applicationId, CancellationToken cancellationToken = default);

    /// <summary>The only HR/IT-side task in the checklist - the rest are candidate self-service.</summary>
    Task<PreboardingChecklistItemDto?> CompleteItAssetTaskAsync(
        Guid applicationId, CancellationToken cancellationToken = default);

    /// <summary>Self-scoped via ICandidateContext, but still takes applicationId since one
    /// candidate can have several - ownership is verified, never assumed from the id alone.</summary>
    Task<IReadOnlyList<MyPreboardingTaskDto>?> GetMyChecklistAsync(
        Guid applicationId, CancellationToken cancellationToken = default);

    /// <summary>Only valid for the three document-upload task types - fails for BankDetails,
    /// ItAssetRequest, or WelcomeCommunication.</summary>
    Task<bool> SubmitDocumentTaskAsync(
        Guid applicationId,
        PreboardingTaskType taskType,
        Stream resumeStream,
        string fileName,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken = default);

    Task<bool> SubmitBankDetailsAsync(
        Guid applicationId, SubmitBankDetailsRequest request, CancellationToken cancellationToken = default);
}
