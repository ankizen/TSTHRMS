using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Employees;
using TSTHRMS.Application.Employees.Dtos;
using TSTHRMS.Domain.Employees;

namespace TSTHRMS.Api.Controllers;

[ApiController]
[Route("api/employees/{employeeId:guid}/previous-employment")]
[Authorize]
public class PreviousEmploymentController(IPreviousEmploymentService previousEmploymentService) : ControllerBase
{
    private const string HrWriteRoles = $"{RoleNames.HRAdmin},{RoleNames.HRBP}";
    private const long MaxUploadSizeBytes = 10 * 1024 * 1024;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PreviousEmploymentRecordDto>>> GetList(
        Guid employeeId, CancellationToken cancellationToken)
    {
        var records = await previousEmploymentService.GetForEmployeeAsync(employeeId, cancellationToken);
        return Ok(records);
    }

    [HttpPost]
    [Authorize(Roles = HrWriteRoles)]
    public async Task<ActionResult<PreviousEmploymentRecordDto>> Create(
        Guid employeeId, PreviousEmploymentRecordWriteRequest request, CancellationToken cancellationToken)
    {
        var record = await previousEmploymentService.CreateAsync(employeeId, request, cancellationToken);
        return record is null ? NotFound() : Ok(record);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = HrWriteRoles)]
    public async Task<ActionResult<PreviousEmploymentRecordDto>> Update(
        Guid employeeId, Guid id, PreviousEmploymentRecordWriteRequest request, CancellationToken cancellationToken)
    {
        var record = await previousEmploymentService.UpdateAsync(employeeId, id, request, cancellationToken);
        return record is null ? NotFound() : Ok(record);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = HrWriteRoles)]
    public async Task<IActionResult> Delete(Guid employeeId, Guid id, CancellationToken cancellationToken)
    {
        var deleted = await previousEmploymentService.DeleteAsync(employeeId, id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/relieving-letter")]
    [Authorize(Roles = HrWriteRoles)]
    [RequestSizeLimit(MaxUploadSizeBytes)]
    public Task<ActionResult<PreviousEmploymentRecordDto>> UploadRelievingLetter(
        Guid employeeId, Guid id, IFormFile file, CancellationToken cancellationToken) =>
        UploadAsync(employeeId, id, PreviousEmploymentDocumentSlot.RelievingLetter, file, cancellationToken);

    [HttpPost("{id:guid}/salary-slip")]
    [Authorize(Roles = HrWriteRoles)]
    [RequestSizeLimit(MaxUploadSizeBytes)]
    public Task<ActionResult<PreviousEmploymentRecordDto>> UploadSalarySlip(
        Guid employeeId, Guid id, IFormFile file, CancellationToken cancellationToken) =>
        UploadAsync(employeeId, id, PreviousEmploymentDocumentSlot.LastSalarySlip, file, cancellationToken);

    private async Task<ActionResult<PreviousEmploymentRecordDto>> UploadAsync(
        Guid employeeId, Guid id, PreviousEmploymentDocumentSlot slot, IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest(new { error = "No file was uploaded." });
        }

        await using var stream = file.OpenReadStream();
        var result = await previousEmploymentService.AttachDocumentAsync(
            employeeId, id, slot, stream, file.FileName, file.ContentType, file.Length, cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return result.Succeeded ? Ok(result.Record) : BadRequest(new { error = result.Error });
    }
}
