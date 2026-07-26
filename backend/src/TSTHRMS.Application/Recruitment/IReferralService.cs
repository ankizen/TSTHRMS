using TSTHRMS.Application.Recruitment.Dtos;

namespace TSTHRMS.Application.Recruitment;

/// <summary>Section 4: an employee refers a candidate from their own ESS login, tagging a job
/// opening. Reuses ICareerSiteService.ApplyAsync's dedupe/candidate-creation logic under the
/// hood, tagged CandidateSource.Referral and attributed to the current user's EmployeeId.</summary>
public interface IReferralService
{
    Task<ApplyResult> SubmitReferralAsync(
        string jobSlug,
        ReferralSubmissionRequest request,
        Stream? resumeStream,
        string? resumeFileName,
        string? resumeContentType,
        long resumeSizeBytes,
        CancellationToken cancellationToken = default);

    /// <summary>Stage-only status per candidate referred, across however many jobs they've
    /// applied to - Section 4: "without exposing full interview feedback".</summary>
    Task<IReadOnlyList<MyReferralDto>> GetMyReferralsAsync(CancellationToken cancellationToken = default);

    /// <summary>HR's payout queue: every candidate whose referral bonus is Payable or has
    /// already been Paid, across every referring employee.</summary>
    Task<IReadOnlyList<ReferralPayoutDto>> GetPayoutsAsync(CancellationToken cancellationToken = default);

    /// <summary>Payable -> Paid only; a no-op guard against double-paying or paying something
    /// that was never eligible in the first place.</summary>
    Task<bool> MarkBonusPaidAsync(Guid candidateId, CancellationToken cancellationToken = default);
}
