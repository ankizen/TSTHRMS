using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TSTHRMS.Application.Recruitment;
using TSTHRMS.Application.Recruitment.Dtos;

namespace TSTHRMS.Api.Controllers;

/// <summary>
/// Section 14's Interviewer self-service surface - deliberately not role-restricted, since any
/// logged-in user can be assigned as a panelist (IInterviewService scopes everything to "am I
/// actually assigned to this interview" instead).
/// </summary>
[ApiController]
[Authorize]
[Route("api/recruitment/my-interviews")]
public class MyInterviewsController(IInterviewService interviewService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MyInterviewDto>>> GetMine(CancellationToken cancellationToken)
    {
        var interviews = await interviewService.GetMyInterviewsAsync(cancellationToken);
        return Ok(interviews);
    }

    [HttpPost("{interviewId:guid}/scorecard")]
    public async Task<ActionResult<InterviewScorecardDto>> SubmitScorecard(
        Guid interviewId, SubmitScorecardRequest request, CancellationToken cancellationToken)
    {
        var scorecard = await interviewService.SubmitScorecardAsync(interviewId, request, cancellationToken);
        return scorecard is null ? BadRequest(new { error = "You can't submit feedback for this interview." }) : Ok(scorecard);
    }
}
