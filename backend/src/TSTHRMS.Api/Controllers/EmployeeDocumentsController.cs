using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Documents;
using TSTHRMS.Application.Documents.Dtos;
using TSTHRMS.Domain.Documents;

namespace TSTHRMS.Api.Controllers;

/// <summary>
/// Section 10's access note is stricter than the rest of Core HR: "only HR Admin and the
/// employee themself" - notably not HRBP, unlike the create/edit endpoints elsewhere. Employee
/// self-access isn't wired up here since it needs the ESS login flow (a later phase); HRAdmin
/// is the only role granted access for now.
/// </summary>
[ApiController]
[Route("api/employees/{employeeId:guid}/documents")]
[Authorize(Roles = RoleNames.HRAdmin)]
public class EmployeeDocumentsController(IDocumentRepositoryService documentRepositoryService) : ControllerBase
{
    private const long MaxUploadSizeBytes = 10 * 1024 * 1024;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DocumentSummaryDto>>> GetList(
        Guid employeeId, CancellationToken cancellationToken)
    {
        var documents = await documentRepositoryService.GetForEmployeeAsync(employeeId, cancellationToken);
        return Ok(documents);
    }

    [HttpPost]
    [RequestSizeLimit(MaxUploadSizeBytes)]
    public async Task<ActionResult<DocumentSummaryDto>> Upload(
        Guid employeeId,
        [FromForm] EmployeeDocumentCategory category,
        [FromForm] string? notes,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest(new { error = "No file was uploaded." });
        }

        await using var stream = file.OpenReadStream();
        var result = await documentRepositoryService.UploadAsync(
            employeeId, new EmployeeDocumentWriteRequest(category, notes),
            stream, file.FileName, file.ContentType, file.Length, cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return result.Succeeded ? Ok(result.Record) : BadRequest(new { error = result.Error });
    }

    [HttpDelete("{employeeDocumentId:guid}")]
    public async Task<IActionResult> Delete(Guid employeeId, Guid employeeDocumentId, CancellationToken cancellationToken)
    {
        var deleted = await documentRepositoryService.DeleteAsync(employeeId, employeeDocumentId, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
