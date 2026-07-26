using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Recruitment;
using TSTHRMS.Application.Recruitment.Dtos;

namespace TSTHRMS.Api.Controllers;

/// <summary>
/// Section 11. Converting a candidate is HRAdmin/HRBP-only (it creates a real Core HR employee
/// record) - a stricter action-level [Authorize] than the class-level list, which additionally
/// lets a Manager view/complete the resulting checklist if they're the new hire's own reporting
/// manager (enforced inside IOnboardingService, not by role alone).
/// </summary>
[ApiController]
[Authorize(Roles = $"{RoleNames.HRAdmin},{RoleNames.HRBP},{RoleNames.Manager}")]
public class OnboardingController(IOnboardingService onboardingService) : ControllerBase
{
    [HttpPost("api/recruitment/applications/{applicationId:guid}/convert-to-employee")]
    [Authorize(Roles = $"{RoleNames.HRAdmin},{RoleNames.HRBP}")]
    public async Task<ActionResult<ConvertToEmployeeResult>> ConvertToEmployee(
        Guid applicationId, CancellationToken cancellationToken)
    {
        var result = await onboardingService.ConvertToEmployeeAsync(applicationId, cancellationToken);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpGet("api/recruitment/employees/{employeeId:guid}/onboarding-checklist")]
    public async Task<ActionResult<IReadOnlyList<OnboardingChecklistItemDto>>> GetChecklist(
        Guid employeeId, CancellationToken cancellationToken)
    {
        var checklist = await onboardingService.GetChecklistAsync(employeeId, cancellationToken);
        return checklist is null ? NotFound() : Ok(checklist);
    }

    [HttpPut("api/recruitment/onboarding-checklist/{itemId:guid}")]
    public async Task<ActionResult<OnboardingChecklistItemDto>> UpdateItem(
        Guid itemId, UpdateOnboardingItemRequest request, CancellationToken cancellationToken)
    {
        var item = await onboardingService.UpdateItemAsync(itemId, request, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost("api/recruitment/onboarding-checklist/{itemId:guid}/complete")]
    public async Task<ActionResult<OnboardingChecklistItemDto>> CompleteItem(Guid itemId, CancellationToken cancellationToken)
    {
        var item = await onboardingService.CompleteItemAsync(itemId, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }
}
