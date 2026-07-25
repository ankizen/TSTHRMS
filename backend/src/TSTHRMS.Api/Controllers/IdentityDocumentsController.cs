using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Employees;
using TSTHRMS.Application.Employees.Dtos;

namespace TSTHRMS.Api.Controllers;

[ApiController]
[Route("api/employees/{employeeId:guid}/identity-documents")]
[Authorize]
public class IdentityDocumentsController(IIdentityDocumentService identityDocumentService) : ControllerBase
{
    private const string HrWriteRoles = $"{RoleNames.HRAdmin},{RoleNames.HRBP}";
    private const long MaxUploadSizeBytes = 10 * 1024 * 1024;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<IdentityDocumentDto>>> GetList(
        Guid employeeId, CancellationToken cancellationToken)
    {
        var documents = await identityDocumentService.GetForEmployeeAsync(employeeId, cancellationToken);
        return Ok(documents);
    }

    [HttpPost]
    [Authorize(Roles = HrWriteRoles)]
    public async Task<ActionResult<IdentityDocumentDto>> Create(
        Guid employeeId, IdentityDocumentWriteRequest request, CancellationToken cancellationToken)
    {
        var result = await identityDocumentService.CreateAsync(employeeId, request, cancellationToken);
        if (result is null)
        {
            return NotFound();
        }

        return result.Succeeded ? Ok(result.Record) : Conflict(new { error = result.Error });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = HrWriteRoles)]
    public async Task<ActionResult<IdentityDocumentDto>> Update(
        Guid employeeId, Guid id, IdentityDocumentWriteRequest request, CancellationToken cancellationToken)
    {
        var document = await identityDocumentService.UpdateAsync(employeeId, id, request, cancellationToken);
        return document is null ? NotFound() : Ok(document);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = HrWriteRoles)]
    public async Task<IActionResult> Delete(Guid employeeId, Guid id, CancellationToken cancellationToken)
    {
        var deleted = await identityDocumentService.DeleteAsync(employeeId, id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/proof")]
    [Authorize(Roles = HrWriteRoles)]
    [RequestSizeLimit(MaxUploadSizeBytes)]
    public async Task<ActionResult<IdentityDocumentDto>> UploadProof(
        Guid employeeId, Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest(new { error = "No file was uploaded." });
        }

        await using var stream = file.OpenReadStream();
        var result = await identityDocumentService.AttachProofAsync(
            employeeId, id, stream, file.FileName, file.ContentType, file.Length, cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return result.Succeeded ? Ok(result.Record) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/reveal")]
    [Authorize(Roles = HrWriteRoles)]
    public async Task<ActionResult<IdentityNumberRevealDto>> Reveal(
        Guid employeeId, Guid id, CancellationToken cancellationToken)
    {
        var result = await identityDocumentService.RevealNumberAsync(employeeId, id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
