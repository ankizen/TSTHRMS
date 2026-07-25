using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Employees;
using TSTHRMS.Application.Employees.Dtos;

namespace TSTHRMS.Api.Controllers;

/// <summary>Section 13: bulk create employees from a spreadsheet. Same write-access level as the
/// single-employee create/update endpoints (HRAdmin + HRBP).</summary>
[ApiController]
[Route("api/employees/bulk-import")]
[Authorize(Roles = $"{RoleNames.HRAdmin},{RoleNames.HRBP}")]
public class EmployeeBulkImportController(IEmployeeBulkImportService bulkImportService) : ControllerBase
{
    private const long MaxUploadSizeBytes = 10 * 1024 * 1024;

    [HttpGet("template")]
    public IActionResult GetTemplate()
    {
        var bytes = bulkImportService.GetTemplate();
        const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        return File(bytes, contentType, "employee-bulk-import-template.xlsx");
    }

    [HttpPost("validate")]
    [RequestSizeLimit(MaxUploadSizeBytes)]
    public async Task<ActionResult<BulkImportSummaryDto>> Validate(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest(new { error = "No file was uploaded." });
        }

        await using var stream = file.OpenReadStream();
        var summary = await bulkImportService.ValidateAsync(stream, cancellationToken);
        return Ok(summary);
    }

    [HttpPost("commit")]
    [RequestSizeLimit(MaxUploadSizeBytes)]
    public async Task<ActionResult<BulkImportSummaryDto>> Commit(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest(new { error = "No file was uploaded." });
        }

        await using var stream = file.OpenReadStream();
        var summary = await bulkImportService.CommitAsync(stream, cancellationToken);
        return Ok(summary);
    }
}
