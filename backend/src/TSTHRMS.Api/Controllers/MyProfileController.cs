using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Employees;
using TSTHRMS.Application.Employees.Dtos;

namespace TSTHRMS.Api.Controllers;

/// <summary>
/// Section 14: Employee and Manager self-service. Always resolves "who" from the caller's own
/// employee_id claim - there's no route id to spoof, unlike EmployeesController.
/// </summary>
[ApiController]
[Route("api/my")]
[Authorize(Roles = $"{RoleNames.Employee},{RoleNames.Manager},{RoleNames.HRAdmin},{RoleNames.HRBP}")]
public class MyProfileController(
    IMyProfileService myProfileService,
    IEmployeeEditRequestService editRequestService) : ControllerBase
{
    [HttpGet("profile")]
    public async Task<ActionResult<EmployeeDto>> GetProfile(CancellationToken cancellationToken)
    {
        var profile = await myProfileService.GetOwnProfileAsync(cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpGet("direct-reports")]
    public async Task<ActionResult<IReadOnlyList<DirectReportSummaryDto>>> GetDirectReports(CancellationToken cancellationToken)
    {
        var reports = await myProfileService.GetDirectReportsAsync(cancellationToken);
        return Ok(reports);
    }

    [HttpPost("edit-requests")]
    public async Task<ActionResult<IReadOnlyList<EmployeeEditRequestDto>>> SubmitEditRequests(
        SubmitEditRequestsRequest request, CancellationToken cancellationToken)
    {
        var created = await editRequestService.SubmitAsync(request, cancellationToken);
        return Ok(created);
    }

    [HttpGet("edit-requests")]
    public async Task<ActionResult<IReadOnlyList<EmployeeEditRequestDto>>> GetOwnEditRequests(CancellationToken cancellationToken)
    {
        var requests = await editRequestService.GetOwnRequestsAsync(cancellationToken);
        return Ok(requests);
    }
}
