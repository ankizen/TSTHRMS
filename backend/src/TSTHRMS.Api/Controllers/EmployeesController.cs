using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Employees;
using TSTHRMS.Application.Employees.Dtos;
using TSTHRMS.Domain.Employees;

namespace TSTHRMS.Api.Controllers;

/// <summary>
/// Section 14: full employee records are an HR-only surface. Managers see their direct reports
/// (restricted fields) and Employees see their own record via the separate /api/my endpoints
/// (MyProfileController) instead of these - not just hidden in the UI, but unreachable here.
/// </summary>
[ApiController]
[Route("api/employees")]
[Authorize(Roles = $"{RoleNames.HRAdmin},{RoleNames.HRBP}")]
public class EmployeesController(IEmployeeService employeeService) : ControllerBase
{
    private const string HrWriteRoles = $"{RoleNames.HRAdmin},{RoleNames.HRBP}";

    [HttpGet]
    public async Task<ActionResult<PagedResult<EmployeeListItemDto>>> GetList(
        [FromQuery] EmployeeListFilter filter, CancellationToken cancellationToken = default)
    {
        var result = await employeeService.GetListAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] EmployeeListFilter filter, CancellationToken cancellationToken)
    {
        var bytes = await employeeService.ExportToExcelAsync(filter, cancellationToken);
        const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        return File(bytes, contentType, $"employees-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx");
    }

    [HttpGet("org-chart")]
    public async Task<ActionResult<IReadOnlyList<OrgChartNodeDto>>> GetOrgChart(
        [FromQuery] Guid? legalEntityId, [FromQuery] Guid? productId, CancellationToken cancellationToken)
    {
        var nodes = await employeeService.GetOrgChartAsync(legalEntityId, productId, cancellationToken);
        return Ok(nodes);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EmployeeDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var employee = await employeeService.GetByIdAsync(id, cancellationToken);
        return employee is null ? NotFound() : Ok(employee);
    }

    [HttpPost]
    [Authorize(Roles = HrWriteRoles)]
    public async Task<ActionResult<EmployeeDto>> Create(EmployeeWriteRequest request, CancellationToken cancellationToken)
    {
        var employee = await employeeService.CreateAsync(request, cancellationToken);
        return employee is null ? Forbid() : CreatedAtAction(nameof(GetById), new { id = employee.Id }, employee);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = HrWriteRoles)]
    public async Task<ActionResult<EmployeeDto>> Update(Guid id, EmployeeWriteRequest request, CancellationToken cancellationToken)
    {
        var employee = await employeeService.UpdateAsync(id, request, cancellationToken);
        return employee is null ? NotFound() : Ok(employee);
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = HrWriteRoles)]
    public async Task<ActionResult<EmployeeDto>> UpdateStatus(
        Guid id, UpdateEmployeeStatusRequest request, CancellationToken cancellationToken)
    {
        var employee = await employeeService.UpdateStatusAsync(id, request.Status, cancellationToken);
        return employee is null ? NotFound() : Ok(employee);
    }

    [HttpPost("{id:guid}/reveal-bank-account")]
    [Authorize(Roles = HrWriteRoles)]
    public async Task<ActionResult<BankAccountRevealDto>> RevealBankAccount(Guid id, CancellationToken cancellationToken)
    {
        var result = await employeeService.RevealBankAccountNumberAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:guid}/posh-acknowledgment")]
    [Authorize(Roles = HrWriteRoles)]
    public async Task<ActionResult<EmployeeDto>> AcknowledgePoshPolicy(Guid id, CancellationToken cancellationToken)
    {
        var employee = await employeeService.AcknowledgePoshPolicyAsync(id, cancellationToken);
        return employee is null ? NotFound() : Ok(employee);
    }

    [HttpPost("{id:guid}/confirm")]
    [Authorize(Roles = HrWriteRoles)]
    public async Task<ActionResult<EmployeeDto>> Confirm(Guid id, ConfirmEmployeeRequest request, CancellationToken cancellationToken)
    {
        var employee = await employeeService.ConfirmAsync(id, request, cancellationToken);
        return employee is null ? NotFound() : Ok(employee);
    }
}
