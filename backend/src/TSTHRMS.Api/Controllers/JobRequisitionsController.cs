using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Recruitment;
using TSTHRMS.Application.Recruitment.Dtos;
using TSTHRMS.Domain.Recruitment;

namespace TSTHRMS.Api.Controllers;

/// <summary>
/// Section 2. A Manager (the PDF's "Hiring Manager") can raise and manage their own
/// requisitions; only HRAdmin/HRBP can decide, publish, hold, resume, or close one - the
/// approval gate itself. IJobRequisitionService enforces the per-requisition ownership scoping,
/// same shape as EmployeeService.ApplyHrbpScope.
/// </summary>
[ApiController]
[Route("api/recruitment/requisitions")]
[Authorize(Roles = $"{RoleNames.HRAdmin},{RoleNames.HRBP},{RoleNames.Manager}")]
public class JobRequisitionsController(IJobRequisitionService requisitionService) : ControllerBase
{
    private const string ApprovalRoles = $"{RoleNames.HRAdmin},{RoleNames.HRBP}";

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<JobRequisitionListItemDto>>> GetList(
        [FromQuery] RequisitionStatus? status, CancellationToken cancellationToken)
    {
        var list = await requisitionService.GetListAsync(status, cancellationToken);
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobRequisitionDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var requisition = await requisitionService.GetByIdAsync(id, cancellationToken);
        return requisition is null ? NotFound() : Ok(requisition);
    }

    [HttpPost]
    public async Task<ActionResult<JobRequisitionDto>> Create(
        JobRequisitionWriteRequest request, CancellationToken cancellationToken)
    {
        var requisition = await requisitionService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = requisition.Id }, requisition);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<JobRequisitionDto>> Update(
        Guid id, JobRequisitionWriteRequest request, CancellationToken cancellationToken)
    {
        var requisition = await requisitionService.UpdateAsync(id, request, cancellationToken);
        return requisition is null ? NotFound() : Ok(requisition);
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<ActionResult<JobRequisitionDto>> Submit(Guid id, CancellationToken cancellationToken)
    {
        var requisition = await requisitionService.SubmitForApprovalAsync(id, cancellationToken);
        return requisition is null ? NotFound() : Ok(requisition);
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = ApprovalRoles)]
    public async Task<ActionResult<JobRequisitionDto>> Approve(
        Guid id, RequisitionDecisionRequest request, CancellationToken cancellationToken)
    {
        var requisition = await requisitionService.DecideAsync(
            id, RequisitionApprovalDecision.Approved, request, cancellationToken);
        return requisition is null ? NotFound() : Ok(requisition);
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = ApprovalRoles)]
    public async Task<ActionResult<JobRequisitionDto>> Reject(
        Guid id, RequisitionDecisionRequest request, CancellationToken cancellationToken)
    {
        var requisition = await requisitionService.DecideAsync(
            id, RequisitionApprovalDecision.Rejected, request, cancellationToken);
        return requisition is null ? NotFound() : Ok(requisition);
    }

    [HttpPost("{id:guid}/hold")]
    [Authorize(Roles = ApprovalRoles)]
    public async Task<ActionResult<JobRequisitionDto>> PutOnHold(Guid id, CancellationToken cancellationToken)
    {
        var requisition = await requisitionService.PutOnHoldAsync(id, cancellationToken);
        return requisition is null ? NotFound() : Ok(requisition);
    }

    [HttpPost("{id:guid}/resume")]
    [Authorize(Roles = ApprovalRoles)]
    public async Task<ActionResult<JobRequisitionDto>> Resume(Guid id, CancellationToken cancellationToken)
    {
        var requisition = await requisitionService.ResumeAsync(id, cancellationToken);
        return requisition is null ? NotFound() : Ok(requisition);
    }

    [HttpPost("{id:guid}/close")]
    [Authorize(Roles = ApprovalRoles)]
    public async Task<ActionResult<JobRequisitionDto>> Close(Guid id, CancellationToken cancellationToken)
    {
        var requisition = await requisitionService.CloseAsync(id, cancellationToken);
        return requisition is null ? NotFound() : Ok(requisition);
    }

    [HttpPost("{id:guid}/publish")]
    [Authorize(Roles = ApprovalRoles)]
    public async Task<ActionResult<JobRequisitionDto>> Publish(
        Guid id, PublishJobPostingRequest request, CancellationToken cancellationToken)
    {
        var requisition = await requisitionService.PublishAsync(id, request, cancellationToken);
        return requisition is null ? BadRequest() : Ok(requisition);
    }
}
