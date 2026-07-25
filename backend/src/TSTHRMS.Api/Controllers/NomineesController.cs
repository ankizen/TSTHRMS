using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Employees;
using TSTHRMS.Application.Employees.Dtos;

namespace TSTHRMS.Api.Controllers;

[ApiController]
[Route("api/employees/{employeeId:guid}/nominees")]
[Authorize]
public class NomineesController(INomineeService nomineeService) : ControllerBase
{
    private const string HrWriteRoles = $"{RoleNames.HRAdmin},{RoleNames.HRBP}";
    private const long MaxUploadSizeBytes = 10 * 1024 * 1024;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NomineeDto>>> GetList(
        Guid employeeId, CancellationToken cancellationToken)
    {
        var nominees = await nomineeService.GetForEmployeeAsync(employeeId, cancellationToken);
        return Ok(nominees);
    }

    [HttpPost]
    [Authorize(Roles = HrWriteRoles)]
    public async Task<ActionResult<NomineeDto>> Create(
        Guid employeeId, NomineeWriteRequest request, CancellationToken cancellationToken)
    {
        var result = await nomineeService.CreateAsync(employeeId, request, cancellationToken);
        if (result is null)
        {
            return NotFound();
        }

        return result.Succeeded ? Ok(result.Record) : BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = HrWriteRoles)]
    public async Task<ActionResult<NomineeDto>> Update(
        Guid employeeId, Guid id, NomineeWriteRequest request, CancellationToken cancellationToken)
    {
        var result = await nomineeService.UpdateAsync(employeeId, id, request, cancellationToken);
        if (result is null)
        {
            return NotFound();
        }

        return result.Succeeded ? Ok(result.Record) : BadRequest(new { error = result.Error });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = HrWriteRoles)]
    public async Task<IActionResult> Delete(Guid employeeId, Guid id, CancellationToken cancellationToken)
    {
        var deleted = await nomineeService.DeleteAsync(employeeId, id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/consent-document")]
    [Authorize(Roles = HrWriteRoles)]
    [RequestSizeLimit(MaxUploadSizeBytes)]
    public async Task<ActionResult<NomineeDto>> UploadConsentDocument(
        Guid employeeId, Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest(new { error = "No file was uploaded." });
        }

        await using var stream = file.OpenReadStream();
        var result = await nomineeService.AttachConsentDocumentAsync(
            employeeId, id, stream, file.FileName, file.ContentType, file.Length, cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return result.Succeeded ? Ok(result.Record) : BadRequest(new { error = result.Error });
    }
}
