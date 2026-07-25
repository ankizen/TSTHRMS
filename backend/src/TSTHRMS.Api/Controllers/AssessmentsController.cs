using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Recruitment;
using TSTHRMS.Application.Recruitment.Dtos;

namespace TSTHRMS.Api.Controllers;

/// <summary>
/// Section 6's internal side: configuring a posting's test, sending it to a candidate, and
/// manually scoring a submission. Same HR/Manager ownership scoping as the other internal
/// recruitment controllers.
/// </summary>
[ApiController]
[Authorize(Roles = $"{RoleNames.HRAdmin},{RoleNames.HRBP},{RoleNames.Manager}")]
public class AssessmentsController(IAssessmentService assessmentService) : ControllerBase
{
    [HttpGet("api/recruitment/job-postings/{jobPostingId:guid}/test-configuration")]
    public async Task<ActionResult<TestConfigurationDto>> GetTestConfiguration(
        Guid jobPostingId, CancellationToken cancellationToken)
    {
        var config = await assessmentService.GetTestConfigurationAsync(jobPostingId, cancellationToken);
        return config is null ? NotFound() : Ok(config);
    }

    [HttpPut("api/recruitment/job-postings/{jobPostingId:guid}/test-configuration")]
    public async Task<ActionResult<TestConfigurationDto>> ConfigureTest(
        Guid jobPostingId, TestConfigurationRequest request, CancellationToken cancellationToken)
    {
        var config = await assessmentService.ConfigureTestAsync(jobPostingId, request, cancellationToken);
        return config is null ? NotFound() : Ok(config);
    }

    [HttpPost("api/recruitment/applications/{applicationId:guid}/assessment")]
    public async Task<ActionResult<SendAssessmentResult>> Send(Guid applicationId, CancellationToken cancellationToken)
    {
        var result = await assessmentService.SendAssessmentAsync(applicationId, cancellationToken);
        return result.Succeeded ? Ok(result) : BadRequest(new { error = result.Error });
    }

    [HttpGet("api/recruitment/assessments/{assessmentSubmissionId:guid}")]
    public async Task<ActionResult<AssessmentDetailDto>> GetDetail(
        Guid assessmentSubmissionId, CancellationToken cancellationToken)
    {
        var detail = await assessmentService.GetDetailAsync(assessmentSubmissionId, cancellationToken);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpPost("api/recruitment/assessments/{assessmentSubmissionId:guid}/score")]
    public async Task<ActionResult<AssessmentDetailDto>> Score(
        Guid assessmentSubmissionId, ScoreAssessmentRequest request, CancellationToken cancellationToken)
    {
        var detail = await assessmentService.ScoreAsync(assessmentSubmissionId, request, cancellationToken);
        return detail is null ? NotFound() : Ok(detail);
    }
}
