using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Recruitment;
using TSTHRMS.Application.Recruitment.Dtos;

namespace TSTHRMS.Api.Controllers;

/// <summary>
/// Section 4: any logged-in employee (ESS) can refer a candidate - not role-restricted, since
/// referring isn't an HR-only action. IReferralService resolves the referring employee from the
/// current user's own employee_id claim, not a parameter, so this can't be used to attribute a
/// referral to someone else.
/// </summary>
[ApiController]
[Authorize]
[Route("api/referrals")]
public class ReferralsController(IReferralService referralService, ICareerSiteService careerSiteService) : ControllerBase
{
    private const long MaxResumeSizeBytes = 10 * 1024 * 1024;

    [HttpGet("jobs")]
    public async Task<ActionResult<IReadOnlyList<PublicJobListItemDto>>> GetOpenJobs(CancellationToken cancellationToken)
    {
        var jobs = await careerSiteService.GetPublishedJobsAsync(
            new PublicJobFilter(null, null, null, null), cancellationToken);
        return Ok(jobs);
    }

    [HttpPost("jobs/{jobSlug}")]
    [RequestSizeLimit(MaxResumeSizeBytes)]
    public async Task<ActionResult<ApplyResult>> Submit(
        string jobSlug, [FromForm] ReferralSubmissionRequest request, IFormFile? resume, CancellationToken cancellationToken)
    {
        await using var stream = resume?.OpenReadStream();
        var result = await referralService.SubmitReferralAsync(
            jobSlug, request, stream, resume?.FileName, resume?.ContentType, resume?.Length ?? 0, cancellationToken);

        return result.Succeeded ? Ok(result) : BadRequest(new { error = result.Error });
    }

    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<MyReferralDto>>> GetMine(CancellationToken cancellationToken)
    {
        var referrals = await referralService.GetMyReferralsAsync(cancellationToken);
        return Ok(referrals);
    }

    [HttpGet("payouts")]
    [Authorize(Roles = $"{RoleNames.HRAdmin},{RoleNames.HRBP}")]
    public async Task<ActionResult<IReadOnlyList<ReferralPayoutDto>>> GetPayouts(CancellationToken cancellationToken)
    {
        var payouts = await referralService.GetPayoutsAsync(cancellationToken);
        return Ok(payouts);
    }

    [HttpPost("payouts/{candidateId:guid}/mark-paid")]
    [Authorize(Roles = $"{RoleNames.HRAdmin},{RoleNames.HRBP}")]
    public async Task<IActionResult> MarkPaid(Guid candidateId, CancellationToken cancellationToken)
    {
        var succeeded = await referralService.MarkBonusPaidAsync(candidateId, cancellationToken);
        return succeeded ? NoContent() : BadRequest(new { error = "This candidate's referral bonus isn't Payable." });
    }
}
