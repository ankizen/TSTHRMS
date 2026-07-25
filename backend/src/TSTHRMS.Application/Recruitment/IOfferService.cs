using TSTHRMS.Application.Recruitment.Dtos;

namespace TSTHRMS.Application.Recruitment;

public interface IOfferService
{
    /// <summary>Null means the application wasn't found, the caller lacks access (same
    /// ownership scoping as the other internal recruitment services), or an offer already
    /// exists for this application - revise it instead of creating a second one.</summary>
    Task<OfferDto?> CreateAsync(
        Guid applicationId, CreateOrReviseOfferRequest request, CancellationToken cancellationToken = default);

    /// <summary>Adds a new immutable OfferVersion and resets Status to Draft - any revision
    /// needs to clear the approval gate again (Section 8: sign-off matters most "especially if
    /// it deviates from the requisition budget", so a changed offer can't skip re-approval).</summary>
    Task<OfferDto?> ReviseAsync(
        Guid offerId, CreateOrReviseOfferRequest request, CancellationToken cancellationToken = default);

    Task<OfferDto?> SubmitForApprovalAsync(Guid offerId, CancellationToken cancellationToken = default);

    Task<OfferDto?> ApproveAsync(Guid offerId, OfferDecisionRequest request, CancellationToken cancellationToken = default);

    /// <summary>Approved -> Sent: generates the token, computes ExpiresAt, and emails the
    /// candidate the accept/decline link.</summary>
    Task<OfferDto?> SendAsync(Guid offerId, SendOfferRequest request, CancellationToken cancellationToken = default);

    Task<OfferDto?> GetForApplicationAsync(Guid applicationId, CancellationToken cancellationToken = default);

    /// <summary>Anonymous - resolved by the opaque token under the tenant the public request was
    /// already resolved to. Lazily flips Sent -> Expired on read once past ExpiresAt, since there
    /// is no background job to do it on a schedule.</summary>
    Task<PublicOfferDto?> GetPublicOfferAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>Accepting moves the application to Stage.OfferAccepted; declining moves it to
    /// Stage.Rejected with the decline reason recorded, same as an internal rejection.</summary>
    Task<bool> RespondPublicOfferAsync(
        string token, PublicOfferDecisionRequest request, CancellationToken cancellationToken = default);
}
