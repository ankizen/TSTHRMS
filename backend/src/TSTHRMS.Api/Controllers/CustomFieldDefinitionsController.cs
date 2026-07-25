using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.CustomFields;
using TSTHRMS.Application.CustomFields.Dtos;

namespace TSTHRMS.Api.Controllers;

/// <summary>Section 15: HRAdmin-only configuration of employee custom fields. Read access is
/// wider (HRAdmin + HRBP, matching the rest of Core HR) since HRBP needs to see field
/// definitions to fill in values on the employee form.</summary>
[ApiController]
[Route("api/custom-field-definitions")]
[Authorize(Roles = $"{RoleNames.HRAdmin},{RoleNames.HRBP}")]
public class CustomFieldDefinitionsController(ICustomFieldService customFieldService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomFieldDefinitionDto>>> GetList(CancellationToken cancellationToken)
    {
        var definitions = await customFieldService.GetDefinitionsAsync(cancellationToken);
        return Ok(definitions);
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.HRAdmin)]
    public async Task<ActionResult<CustomFieldDefinitionDto>> Create(
        CustomFieldDefinitionWriteRequest request, CancellationToken cancellationToken)
    {
        var definition = await customFieldService.CreateDefinitionAsync(request, cancellationToken);
        return definition is null
            ? Conflict(new { error = $"A field named '{request.Name}' already exists." })
            : Ok(definition);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = RoleNames.HRAdmin)]
    public async Task<ActionResult<CustomFieldDefinitionDto>> Update(
        Guid id, CustomFieldDefinitionWriteRequest request, CancellationToken cancellationToken)
    {
        var definition = await customFieldService.UpdateDefinitionAsync(id, request, cancellationToken);
        return definition is null
            ? Conflict(new { error = $"A field named '{request.Name}' already exists, or the field wasn't found." })
            : Ok(definition);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = RoleNames.HRAdmin)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await customFieldService.DeleteDefinitionAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
