using Microsoft.EntityFrameworkCore;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Application.Employees.Dtos;

namespace TSTHRMS.Application.Employees;

public class MyProfileService(
    IApplicationDbContext dbContext,
    IEmployeeService employeeService,
    ICurrentUserService currentUserService) : IMyProfileService
{
    public Task<EmployeeDto?> GetOwnProfileAsync(CancellationToken cancellationToken = default) =>
        currentUserService.EmployeeId is { } employeeId
            ? employeeService.GetByIdAsync(employeeId, cancellationToken)
            : Task.FromResult<EmployeeDto?>(null);

    public async Task<IReadOnlyList<DirectReportSummaryDto>> GetDirectReportsAsync(CancellationToken cancellationToken = default)
    {
        if (currentUserService.EmployeeId is not { } managerId)
        {
            return [];
        }

        return await dbContext.Employees
            .AsNoTracking()
            .Where(e => e.ReportingManagerId == managerId)
            .OrderBy(e => e.FirstName).ThenBy(e => e.LastName)
            .Select(e => new DirectReportSummaryDto(
                e.Id, e.EmployeeCode, e.FirstName, e.LastName,
                e.Designation, e.Department, e.WorkLocation, e.Status, e.DateOfJoining))
            .ToListAsync(cancellationToken);
    }
}
