using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Employees;
using TSTHRMS.Application.Employees.Dtos;
using TSTHRMS.Domain.Employees;

namespace TSTHRMS.Api.Controllers;

[ApiController]
[Route("api/employees")]
[Authorize]
public class EmployeesController(IEmployeeService employeeService) : ControllerBase
{
    private const string HrWriteRoles = $"{RoleNames.HRAdmin},{RoleNames.HRBP}";

    [HttpGet]
    public async Task<ActionResult<PagedResult<EmployeeListItemDto>>> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        [FromQuery] EmployeeStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await employeeService.GetListAsync(page, pageSize, search, status, cancellationToken);
        return Ok(result);
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
        return CreatedAtAction(nameof(GetById), new { id = employee.Id }, employee);
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
