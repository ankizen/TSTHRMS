using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TSTHRMS.Application.Auditing;
using TSTHRMS.Application.Auditing.Dtos;
using TSTHRMS.Application.Common;

namespace TSTHRMS.Api.Controllers;

/// <summary>
/// Section 12: read-only change history, HRAdmin-only (same access level as the Document
/// Repository) since it can surface sensitive-field activity across an employee's whole record.
/// </summary>
[ApiController]
[Route("api/employees/{employeeId:guid}/audit-log")]
[Authorize(Roles = RoleNames.HRAdmin)]
public class EmployeeAuditLogController(IAuditLogService auditLogService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AuditLogEntryDto>>> GetHistory(
        Guid employeeId, CancellationToken cancellationToken)
    {
        var history = await auditLogService.GetEmployeeHistoryAsync(employeeId, cancellationToken);
        return history is null ? NotFound() : Ok(history);
    }

    [HttpPost("{auditLogId:guid}/reveal")]
    public async Task<ActionResult<AuditLogEntryDto>> RevealEntry(
        Guid employeeId, Guid auditLogId, CancellationToken cancellationToken)
    {
        var entry = await auditLogService.RevealEntryAsync(employeeId, auditLogId, cancellationToken);
        return entry is null ? NotFound() : Ok(entry);
    }
}
