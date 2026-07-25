using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Recruitment;
using TSTHRMS.Application.Recruitment.Dtos;

namespace TSTHRMS.Api.Controllers;

/// <summary>
/// Section 7's scheduling side, restricted to HR/Manager - same ownership scoping as
/// ApplicantsController (a Manager only reaches applications under their own requisitions).
/// Submitting a scorecard and the self-service "my interviews" view live in
/// MyInterviewsController instead, since any logged-in user can be an assigned interviewer
/// (Section 14), not just these three roles.
/// </summary>
[ApiController]
[Authorize(Roles = $"{RoleNames.HRAdmin},{RoleNames.HRBP},{RoleNames.Manager}")]
public class InterviewsController(IInterviewService interviewService) : ControllerBase
{
    [HttpGet("api/recruitment/applications/{applicationId:guid}/interviews")]
    public async Task<ActionResult<IReadOnlyList<InterviewDto>>> GetForApplication(
        Guid applicationId, CancellationToken cancellationToken)
    {
        var interviews = await interviewService.GetForApplicationAsync(applicationId, cancellationToken);
        return interviews is null ? NotFound() : Ok(interviews);
    }

    [HttpPost("api/recruitment/applications/{applicationId:guid}/interviews")]
    public async Task<ActionResult<InterviewDto>> Schedule(
        Guid applicationId, ScheduleInterviewRequest request, CancellationToken cancellationToken)
    {
        var interview = await interviewService.ScheduleAsync(applicationId, request, cancellationToken);
        return interview is null ? NotFound() : Ok(interview);
    }

    [HttpPost("api/recruitment/interviews/{interviewId:guid}/reschedule")]
    public async Task<ActionResult<InterviewDto>> Reschedule(
        Guid interviewId, RescheduleInterviewRequest request, CancellationToken cancellationToken)
    {
        var interview = await interviewService.RescheduleAsync(interviewId, request, cancellationToken);
        return interview is null ? NotFound() : Ok(interview);
    }

    [HttpPost("api/recruitment/interviews/{interviewId:guid}/status")]
    public async Task<ActionResult<InterviewDto>> UpdateStatus(
        Guid interviewId, UpdateInterviewStatusRequest request, CancellationToken cancellationToken)
    {
        var interview = await interviewService.UpdateStatusAsync(interviewId, request, cancellationToken);
        return interview is null ? NotFound() : Ok(interview);
    }

    [HttpGet("api/recruitment/interviewer-candidates")]
    public async Task<ActionResult<IReadOnlyList<InterviewerCandidateDto>>> GetInterviewerCandidates(
        CancellationToken cancellationToken)
    {
        var candidates = await interviewService.GetInterviewerCandidatesAsync(cancellationToken);
        return Ok(candidates);
    }
}
