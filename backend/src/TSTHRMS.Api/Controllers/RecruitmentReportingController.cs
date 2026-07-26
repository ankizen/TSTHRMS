using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Recruitment;
using TSTHRMS.Application.Recruitment.Dtos;

namespace TSTHRMS.Api.Controllers;

/// <summary>
/// Section 12. A Manager sees the same slice of the pipeline reporting shows them elsewhere -
/// only requisitions they raised and everything downstream (IRecruitmentReportingService
/// enforces this, same ownership-scoping shape as JobRequisitionService).
/// </summary>
[ApiController]
[Route("api/recruitment/reports")]
[Authorize(Roles = $"{RoleNames.HRAdmin},{RoleNames.HRBP},{RoleNames.Manager}")]
public class RecruitmentReportingController(IRecruitmentReportingService reportingService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<RecruitmentReportDto>> GetReport(CancellationToken cancellationToken)
    {
        var report = await reportingService.GetReportAsync(cancellationToken);
        return Ok(report);
    }
}
