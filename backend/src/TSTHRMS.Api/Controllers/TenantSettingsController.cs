using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Recruitment;
using TSTHRMS.Application.Recruitment.Dtos;

namespace TSTHRMS.Api.Controllers;

/// <summary>
/// The "build now, toggle later" tenant-wide recruitment config: rejected-candidate retention
/// (Section 13), referral bonus amount (Section 4), offer letter template (Section 8). HRAdmin
/// only, for both read and write - small and sensitive enough not to need HRBP read access.
/// </summary>
[ApiController]
[Route("api/recruitment/settings")]
[Authorize(Roles = RoleNames.HRAdmin)]
public class TenantSettingsController(ITenantSettingsService tenantSettingsService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<TenantSettingsDto>> Get(CancellationToken cancellationToken)
    {
        var settings = await tenantSettingsService.GetAsync(cancellationToken);
        return Ok(settings);
    }

    [HttpPut]
    public async Task<ActionResult<TenantSettingsDto>> Update(
        UpdateTenantSettingsRequest request, CancellationToken cancellationToken)
    {
        var settings = await tenantSettingsService.UpdateAsync(request, cancellationToken);
        return Ok(settings);
    }
}
