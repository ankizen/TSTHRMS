using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.CustomFields;
using TSTHRMS.Application.CustomFields.Dtos;

namespace TSTHRMS.Api.Controllers;

[ApiController]
[Route("api/employees/{employeeId:guid}/custom-fields")]
[Authorize(Roles = $"{RoleNames.HRAdmin},{RoleNames.HRBP}")]
public class EmployeeCustomFieldsController(ICustomFieldService customFieldService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EmployeeCustomFieldValueDto>>> GetValues(
        Guid employeeId, CancellationToken cancellationToken)
    {
        var values = await customFieldService.GetValuesForEmployeeAsync(employeeId, cancellationToken);
        return values is null ? NotFound() : Ok(values);
    }

    [HttpPut]
    public async Task<ActionResult<IReadOnlyList<EmployeeCustomFieldValueDto>>> SetValues(
        Guid employeeId, SetEmployeeCustomFieldValuesRequest request, CancellationToken cancellationToken)
    {
        var values = await customFieldService.SetValuesForEmployeeAsync(employeeId, request, cancellationToken);
        return values is null ? NotFound() : Ok(values);
    }
}
