using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Recruitment;
using TSTHRMS.Application.Recruitment.Dtos;

namespace TSTHRMS.Api.Controllers;

/// <summary>Section 10's HR-facing view of a candidate's pre-boarding checklist, plus the one
/// HR/IT-side task (IT asset request) - everything else is candidate self-service, see
/// CandidatePortalController.</summary>
[ApiController]
[Authorize(Roles = $"{RoleNames.HRAdmin},{RoleNames.HRBP},{RoleNames.Manager}")]
public class PreboardingController(IPreboardingService preboardingService) : ControllerBase
{
    [HttpGet("api/recruitment/applications/{applicationId:guid}/preboarding")]
    public async Task<ActionResult<IReadOnlyList<PreboardingChecklistItemDto>>> GetChecklist(
        Guid applicationId, CancellationToken cancellationToken)
    {
        var checklist = await preboardingService.GetChecklistAsync(applicationId, cancellationToken);
        return checklist is null ? NotFound() : Ok(checklist);
    }

    [HttpPost("api/recruitment/applications/{applicationId:guid}/preboarding/it-asset-request/complete")]
    public async Task<ActionResult<PreboardingChecklistItemDto>> CompleteItAssetTask(
        Guid applicationId, CancellationToken cancellationToken)
    {
        var item = await preboardingService.CompleteItAssetTaskAsync(applicationId, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }
}
