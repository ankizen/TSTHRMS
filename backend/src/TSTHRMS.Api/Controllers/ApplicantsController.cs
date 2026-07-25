using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Recruitment;
using TSTHRMS.Application.Recruitment.Dtos;

namespace TSTHRMS.Api.Controllers;

/// <summary>
/// Section 5's internal pipeline view for one job posting. IApplicantService enforces that a
/// Manager only sees postings raised from their own requisitions (Section 14).
/// </summary>
[ApiController]
[Authorize(Roles = $"{RoleNames.HRAdmin},{RoleNames.HRBP},{RoleNames.Manager}")]
public class ApplicantsController(IApplicantService applicantService) : ControllerBase
{
    [HttpGet("api/recruitment/job-postings/{jobPostingId:guid}/applicants")]
    public async Task<ActionResult<IReadOnlyList<ApplicantListItemDto>>> GetForPosting(
        Guid jobPostingId, CancellationToken cancellationToken)
    {
        var applicants = await applicantService.GetForPostingAsync(jobPostingId, cancellationToken);
        return applicants is null ? NotFound() : Ok(applicants);
    }

    [HttpPatch("api/recruitment/applications/{applicationId:guid}/stage")]
    public async Task<ActionResult<ApplicantListItemDto>> MoveStage(
        Guid applicationId, MoveApplicationStageRequest request, CancellationToken cancellationToken)
    {
        var applicant = await applicantService.MoveStageAsync(applicationId, request, cancellationToken);
        return applicant is null ? NotFound() : Ok(applicant);
    }

    [HttpPost("api/recruitment/candidates/{candidateId:guid}/talent-pool")]
    public async Task<IActionResult> SetTalentPool(
        Guid candidateId, [FromBody] bool isInTalentPool, CancellationToken cancellationToken)
    {
        var success = await applicantService.SetTalentPoolAsync(candidateId, isInTalentPool, cancellationToken);
        return success ? NoContent() : NotFound();
    }

    [HttpGet("api/recruitment/candidates/talent-pool")]
    [Authorize(Roles = $"{RoleNames.HRAdmin},{RoleNames.HRBP}")]
    public async Task<ActionResult<IReadOnlyList<TalentPoolCandidateDto>>> GetTalentPool(CancellationToken cancellationToken)
    {
        var candidates = await applicantService.GetTalentPoolAsync(cancellationToken);
        return Ok(candidates);
    }
}
