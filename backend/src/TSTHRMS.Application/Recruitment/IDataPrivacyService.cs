using TSTHRMS.Application.Recruitment.Dtos;
using TSTHRMS.Domain.Recruitment;

namespace TSTHRMS.Application.Recruitment;

/// <summary>
/// Section 13 (DPDPA 2023): candidate self-service erasure requests plus the automatic retention
/// sweep for Rejected candidates. See CandidateDataDeletionRequest and Candidate.IsAnonymized for
/// the shape - anonymization overwrites PII in place, it never deletes the row, so pipeline
/// history stays intact for audit/reporting.
/// </summary>
public interface IDataPrivacyService
{
    /// <summary>Candidate Portal self-service - resolves the caller via ICandidateContext, not a
    /// parameter, so a candidate can only ever request deletion of their own data.</summary>
    Task<RequestDeletionResult> RequestDeletionAsync(CancellationToken cancellationToken = default);

    /// <summary>The calling candidate's own most recent request, or null if they've never made
    /// one - lets the portal disable the button while one is already Pending.</summary>
    Task<CandidateDataDeletionRequestDto?> GetMyDeletionRequestAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CandidateDataDeletionRequestDto>> GetDeletionRequestsAsync(
        CandidateDataDeletionRequestStatus? status, CancellationToken cancellationToken = default);

    /// <summary>Approving anonymizes immediately - even if the candidate is IsInTalentPool - since
    /// an explicit erasure request always overrides HR wanting to keep them in mind. Refuses if
    /// the candidate has ever reached Hired (their data is now an active employment record, out of
    /// this flow's reach).</summary>
    Task<DecideDeletionRequestResult> DecideDeletionRequestAsync(
        Guid requestId, DecideDeletionRequestRequest request, CancellationToken cancellationToken = default);

    /// <summary>Anonymizes every Rejected, non-talent-pool candidate whose most recent stage
    /// change is older than the tenant's RejectedCandidateRetentionDays. Returns the count
    /// anonymized. Called both by CandidateRetentionHostedService's daily tick and by an
    /// HRAdmin-triggered "run now" endpoint.</summary>
    Task<int> RunRetentionSweepAsync(CancellationToken cancellationToken = default);
}
