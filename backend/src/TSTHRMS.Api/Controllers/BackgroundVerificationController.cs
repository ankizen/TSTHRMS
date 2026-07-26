using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Recruitment;
using TSTHRMS.Application.Recruitment.Dtos;

namespace TSTHRMS.Api.Controllers;

/// <summary>Section 9. Same HR/Manager ownership scoping as the rest of the internal
/// recruitment surface.</summary>
[ApiController]
[Authorize(Roles = $"{RoleNames.HRAdmin},{RoleNames.HRBP},{RoleNames.Manager}")]
public class BackgroundVerificationController(IBackgroundVerificationService bgvService) : ControllerBase
{
    [HttpGet("api/recruitment/applications/{applicationId:guid}/bgv")]
    public async Task<ActionResult<BgvDto>> GetForApplication(Guid applicationId, CancellationToken cancellationToken)
    {
        var bgv = await bgvService.GetForApplicationAsync(applicationId, cancellationToken);
        return bgv is null ? NotFound() : Ok(bgv);
    }

    [HttpPost("api/recruitment/applications/{applicationId:guid}/bgv/initiate")]
    public async Task<ActionResult<BgvDto>> Initiate(
        Guid applicationId, InitiateBgvRequest request, CancellationToken cancellationToken)
    {
        var bgv = await bgvService.InitiateAsync(applicationId, request, cancellationToken);
        return bgv is null ? NotFound() : Ok(bgv);
    }

    [HttpPost("api/recruitment/applications/{applicationId:guid}/bgv/status")]
    public async Task<ActionResult<BgvDto>> UpdateStatus(
        Guid applicationId, UpdateBgvStatusRequest request, CancellationToken cancellationToken)
    {
        var bgv = await bgvService.UpdateStatusAsync(applicationId, request, cancellationToken);
        return bgv is null ? NotFound() : Ok(bgv);
    }
}
