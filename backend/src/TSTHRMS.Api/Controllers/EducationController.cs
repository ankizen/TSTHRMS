using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Employees;
using TSTHRMS.Application.Employees.Dtos;

namespace TSTHRMS.Api.Controllers;

[ApiController]
[Route("api/employees/{employeeId:guid}/education")]
[Authorize]
public class EducationController(IEducationService educationService) : ControllerBase
{
    private const string HrWriteRoles = $"{RoleNames.HRAdmin},{RoleNames.HRBP}";
    private const long MaxUploadSizeBytes = 10 * 1024 * 1024;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EducationRecordDto>>> GetList(
        Guid employeeId, CancellationToken cancellationToken)
    {
        var records = await educationService.GetForEmployeeAsync(employeeId, cancellationToken);
        return Ok(records);
    }

    [HttpPost]
    [Authorize(Roles = HrWriteRoles)]
    public async Task<ActionResult<EducationRecordDto>> Create(
        Guid employeeId, EducationRecordWriteRequest request, CancellationToken cancellationToken)
    {
        var record = await educationService.CreateAsync(employeeId, request, cancellationToken);
        return record is null ? NotFound() : Ok(record);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = HrWriteRoles)]
    public async Task<ActionResult<EducationRecordDto>> Update(
        Guid employeeId, Guid id, EducationRecordWriteRequest request, CancellationToken cancellationToken)
    {
        var record = await educationService.UpdateAsync(employeeId, id, request, cancellationToken);
        return record is null ? NotFound() : Ok(record);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = HrWriteRoles)]
    public async Task<IActionResult> Delete(Guid employeeId, Guid id, CancellationToken cancellationToken)
    {
        var deleted = await educationService.DeleteAsync(employeeId, id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPatch("{id:guid}/verification-status")]
    [Authorize(Roles = HrWriteRoles)]
    public async Task<ActionResult<EducationRecordDto>> UpdateVerificationStatus(
        Guid employeeId, Guid id, UpdateVerificationStatusRequest request, CancellationToken cancellationToken)
    {
        var record = await educationService.UpdateVerificationStatusAsync(
            employeeId, id, request.VerificationStatus, cancellationToken);
        return record is null ? NotFound() : Ok(record);
    }

    [HttpPost("{id:guid}/certificate")]
    [Authorize(Roles = HrWriteRoles)]
    [RequestSizeLimit(MaxUploadSizeBytes)]
    public async Task<ActionResult<EducationRecordDto>> UploadCertificate(
        Guid employeeId, Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest(new { error = "No file was uploaded." });
        }

        await using var stream = file.OpenReadStream();
        var result = await educationService.AttachCertificateAsync(
            employeeId, id, stream, file.FileName, file.ContentType, file.Length, cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return result.Succeeded ? Ok(result.Record) : BadRequest(new { error = result.Error });
    }
}
