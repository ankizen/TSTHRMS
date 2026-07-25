using TSTHRMS.Application.Recruitment.Dtos;
using TSTHRMS.Domain.Recruitment;

namespace TSTHRMS.Application.Recruitment;

/// <summary>
/// The public, anonymous surface (Section 1). Every method here runs under a tenant resolved by
/// slug (see ResolvePublicTenantAttribute) rather than a JWT claim - no authentication involved.
/// </summary>
public interface ICareerSiteService
{
    Task<IReadOnlyList<PublicJobListItemDto>> GetPublishedJobsAsync(
        PublicJobFilter filter, CancellationToken cancellationToken = default);

    Task<PublicJobDetailDto?> GetPublishedJobBySlugAsync(string jobSlug, CancellationToken cancellationToken = default);

    /// <summary>referredByEmployeeId is set only for Section 4's internal referral submission
    /// path (CandidateSource.Referral) - null for every public career-site application.</summary>
    Task<ApplyResult> ApplyAsync(
        string jobSlug,
        PublicApplicationRequest request,
        CandidateSource source,
        Guid? referredByEmployeeId,
        Stream? resumeStream,
        string? resumeFileName,
        string? resumeContentType,
        long resumeSizeBytes,
        CancellationToken cancellationToken = default);
}
