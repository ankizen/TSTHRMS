using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Employees;
using TSTHRMS.Application.Employees.Dtos;

namespace TSTHRMS.Api.Controllers;

[ApiController]
[Route("api/employees/{employeeId:guid}/family")]
[Authorize]
public class FamilyMembersController(IFamilyService familyService) : ControllerBase
{
    private const string HrWriteRoles = $"{RoleNames.HRAdmin},{RoleNames.HRBP}";

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FamilyMemberDto>>> GetList(
        Guid employeeId, CancellationToken cancellationToken)
    {
        var members = await familyService.GetForEmployeeAsync(employeeId, cancellationToken);
        return Ok(members);
    }

    [HttpPost]
    [Authorize(Roles = HrWriteRoles)]
    public async Task<ActionResult<FamilyMemberDto>> Create(
        Guid employeeId, FamilyMemberWriteRequest request, CancellationToken cancellationToken)
    {
        var member = await familyService.CreateAsync(employeeId, request, cancellationToken);
        return member is null ? NotFound() : Ok(member);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = HrWriteRoles)]
    public async Task<ActionResult<FamilyMemberDto>> Update(
        Guid employeeId, Guid id, FamilyMemberWriteRequest request, CancellationToken cancellationToken)
    {
        var member = await familyService.UpdateAsync(employeeId, id, request, cancellationToken);
        return member is null ? NotFound() : Ok(member);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = HrWriteRoles)]
    public async Task<IActionResult> Delete(Guid employeeId, Guid id, CancellationToken cancellationToken)
    {
        var deleted = await familyService.DeleteAsync(employeeId, id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
