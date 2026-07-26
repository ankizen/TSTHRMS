using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Recruitment;
using TSTHRMS.Application.Recruitment.Dtos;
using TSTHRMS.Domain.Recruitment;

namespace TSTHRMS.Api.Controllers;

/// <summary>
/// Section 13 (DPDPA 2023): HR's side of candidate self-service erasure requests, plus a manual
/// "run the retention sweep now" trigger for ops visibility/testing rather than waiting on
/// CandidateRetentionHostedService's daily tick.
/// </summary>
[ApiController]
[Route("api/recruitment/data-privacy")]
[Authorize(Roles = $"{RoleNames.HRAdmin},{RoleNames.HRBP}")]
public class DataPrivacyController(IDataPrivacyService dataPrivacyService) : ControllerBase
{
    [HttpGet("deletion-requests")]
    public async Task<ActionResult<IReadOnlyList<CandidateDataDeletionRequestDto>>> GetDeletionRequests(
        [FromQuery] CandidateDataDeletionRequestStatus? status, CancellationToken cancellationToken)
    {
        var requests = await dataPrivacyService.GetDeletionRequestsAsync(status, cancellationToken);
        return Ok(requests);
    }

    [HttpPost("deletion-requests/{requestId:guid}/decide")]
    public async Task<ActionResult<DecideDeletionRequestResult>> DecideDeletionRequest(
        Guid requestId, DecideDeletionRequestRequest request, CancellationToken cancellationToken)
    {
        var result = await dataPrivacyService.DecideDeletionRequestAsync(requestId, request, cancellationToken);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpPost("run-retention-sweep")]
    [Authorize(Roles = RoleNames.HRAdmin)]
    public async Task<ActionResult<int>> RunRetentionSweep(CancellationToken cancellationToken)
    {
        var anonymizedCount = await dataPrivacyService.RunRetentionSweepAsync(cancellationToken);
        return Ok(anonymizedCount);
    }
}
