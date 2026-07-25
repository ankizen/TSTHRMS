using TSTHRMS.Domain.Employees;

namespace TSTHRMS.Application.Employees.Dtos;

public record OrgChartNodeDto(
    Guid Id,
    string FullName,
    string? Designation,
    string? Department,
    Guid? ReportingManagerId,
    EmployeeStatus Status);
