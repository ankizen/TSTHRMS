using TSTHRMS.Domain.Employees;

namespace TSTHRMS.Application.Employees.Dtos;

/// <summary>Section 14: a Manager's view of a direct report is read-only and field-restricted -
/// no salary, no bank details, no personal address/emergency contact - unlike the full EmployeeDto
/// HR sees.</summary>
public record DirectReportSummaryDto(
    Guid Id,
    string EmployeeCode,
    string FirstName,
    string LastName,
    string? Designation,
    string? Department,
    string? WorkLocation,
    EmployeeStatus Status,
    DateOnly DateOfJoining);
