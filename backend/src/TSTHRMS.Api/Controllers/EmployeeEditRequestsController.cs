using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Employees;
using TSTHRMS.Application.Employees.Dtos;

namespace TSTHRMS.Api.Controllers;

/// <summary>Section 14: HR's review queue for self-service edit requests submitted via
/// /api/my/edit-requests. Same write-access level as everything else in Core HR (HRAdmin + HRBP),
/// with HRBP results narrowed to their assigned legal entity/product like everywhere else.</summary>
[ApiController]
[Route("api/employee-edit-requests")]
[Authorize(Roles = $"{RoleNames.HRAdmin},{RoleNames.HRBP}")]
public class EmployeeEditRequestsController(IEmployeeEditRequestService editRequestService) : ControllerBase
{
    [HttpGet("pending")]
    public async Task<ActionResult<IReadOnlyList<EmployeeEditRequestDto>>> GetPending(CancellationToken cancellationToken)
    {
        var requests = await editRequestService.GetPendingAsync(cancellationToken);
        return Ok(requests);
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<EmployeeEditRequestDto>> Approve(
        Guid id, ReviewEditRequestDto request, CancellationToken cancellationToken)
    {
        var result = await editRequestService.ApproveAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<EmployeeEditRequestDto>> Reject(
        Guid id, ReviewEditRequestDto request, CancellationToken cancellationToken)
    {
        var result = await editRequestService.RejectAsync(id, request, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
