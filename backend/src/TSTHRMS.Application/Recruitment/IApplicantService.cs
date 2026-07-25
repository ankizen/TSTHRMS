using TSTHRMS.Application.Recruitment.Dtos;

namespace TSTHRMS.Application.Recruitment;

public interface IApplicantService
{
    /// <summary>Null means the job posting wasn't found (or belongs to another tenant).</summary>
    Task<IReadOnlyList<ApplicantListItemDto>?> GetForPostingAsync(
        Guid jobPostingId, CancellationToken cancellationToken = default);

    Task<ApplicantListItemDto?> MoveStageAsync(
        Guid applicationId, MoveApplicationStageRequest request, CancellationToken cancellationToken = default);

    /// <summary>Section 5's "Keep in mind" tag for a rejected-but-good candidate. False means the
    /// candidate wasn't found.</summary>
    Task<bool> SetTalentPoolAsync(Guid candidateId, bool isInTalentPool, CancellationToken cancellationToken = default);
}
