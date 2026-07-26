using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TSTHRMS.Application.Recruitment;
using TSTHRMS.Application.Recruitment.Dtos;
using TSTHRMS.Domain.Recruitment;

namespace TSTHRMS.Api.Controllers;

/// <summary>
/// Section 3's Candidate Portal - deliberately not role-restricted, since a candidate's JWT
/// carries no roles at all (see JwtTokenGenerator.GenerateCandidateAccessToken). Every method
/// here is self-scoped (via ICandidateContext, checked inside the services), so there's no id
/// parameter a caller could tamper with to see or modify someone else's data.
/// </summary>
[ApiController]
[Authorize]
[Route("api/candidate-portal")]
public class CandidatePortalController(
    ICandidatePortalService candidatePortalService, IPreboardingService preboardingService,
    IDataPrivacyService dataPrivacyService) : ControllerBase
{
    private const long MaxDocumentSizeBytes = 10 * 1024 * 1024;

    [HttpGet("applications")]
    public async Task<ActionResult<IReadOnlyList<MyApplicationDto>>> GetMyApplications(CancellationToken cancellationToken)
    {
        var applications = await candidatePortalService.GetMyApplicationsAsync(cancellationToken);
        return Ok(applications);
    }

    [HttpGet("applications/{applicationId:guid}/preboarding")]
    public async Task<ActionResult<IReadOnlyList<MyPreboardingTaskDto>>> GetMyPreboardingChecklist(
        Guid applicationId, CancellationToken cancellationToken)
    {
        var checklist = await preboardingService.GetMyChecklistAsync(applicationId, cancellationToken);
        return checklist is null ? NotFound() : Ok(checklist);
    }

    [HttpPost("applications/{applicationId:guid}/preboarding/{taskType}/document")]
    [RequestSizeLimit(MaxDocumentSizeBytes)]
    public async Task<IActionResult> SubmitDocumentTask(
        Guid applicationId, PreboardingTaskType taskType, IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest(new { error = "No file was uploaded." });
        }

        await using var stream = file.OpenReadStream();
        var succeeded = await preboardingService.SubmitDocumentTaskAsync(
            applicationId, taskType, stream, file.FileName, file.ContentType, file.Length, cancellationToken);

        return succeeded ? NoContent() : BadRequest(new { error = "Couldn't submit this document." });
    }

    [HttpPost("applications/{applicationId:guid}/preboarding/bank-details")]
    public async Task<IActionResult> SubmitBankDetails(
        Guid applicationId, SubmitBankDetailsRequest request, CancellationToken cancellationToken)
    {
        var succeeded = await preboardingService.SubmitBankDetailsAsync(applicationId, request, cancellationToken);
        return succeeded ? NoContent() : BadRequest(new { error = "Couldn't submit bank details." });
    }

    /// <summary>Section 13 (DPDPA 2023): the candidate's own "right to erasure" request.</summary>
    [HttpPost("data-deletion-request")]
    public async Task<ActionResult<RequestDeletionResult>> RequestDataDeletion(CancellationToken cancellationToken)
    {
        var result = await dataPrivacyService.RequestDeletionAsync(cancellationToken);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpGet("data-deletion-request")]
    public async Task<ActionResult<CandidateDataDeletionRequestDto>> GetMyDataDeletionRequest(CancellationToken cancellationToken)
    {
        var request = await dataPrivacyService.GetMyDeletionRequestAsync(cancellationToken);
        return request is null ? NotFound() : Ok(request);
    }
}
