using TSTHRMS.Domain.Employees;

namespace TSTHRMS.Application.Employees.Dtos;

public record EmployeeListItemDto(
    Guid Id,
    string EmployeeCode,
    string FirstName,
    string LastName,
    string LegalEntityName,
    string ProductName,
    string? Department,
    string? Designation,
    string? WorkLocation,
    EmployeeStatus Status);

public record EmployeeDto(
    Guid Id,
    string EmployeeCode,
    Guid LegalEntityId,
    string LegalEntityName,
    Guid ProductId,
    string ProductName,
    EmployeeStatus Status,
    string FirstName,
    string LastName,
    Gender Gender,
    DateOnly? DateOfBirth,
    string? PersonalEmail,
    string? PersonalPhone,
    string? CurrentAddress,
    string? PermanentAddress,
    string? EmergencyContactName,
    string? EmergencyContactRelation,
    string? EmergencyContactPhone,
    string? BankAccountNumberMasked,
    string? BankIfscCode,
    DateOnly DateOfJoining,
    string? Designation,
    string? Grade,
    string? Department,
    string? WorkLocation,
    Guid? ReportingManagerId,
    string? ReportingManagerName,
    EmploymentType EmploymentType,
    decimal? MonthlyGrossSalary,
    DateOfBirthProofType? DateOfBirthProofType,
    string? ProfessionalTaxState,
    DateTimeOffset? PoshAcknowledgedAt,
    // Computed, not stored - see ComplianceRules for the derivation and its caveats.
    bool IsPfApplicable,
    bool IsEsicApplicable,
    bool IsMaharashtraLwfEligible,
    bool HasMinorOrDifferentlyAbledDependent,
    DateOnly? ProbationEndDate,
    ConfirmationStatus ConfirmationStatus,
    DateOnly? ConfirmationDate,
    Guid? ConfirmingManagerId,
    string? ConfirmingManagerName,
    DateOnly? ContractStartDate,
    DateOnly? ContractEndDate,
    bool IsContractExpiringSoon);

public record EmployeeWriteRequest(
    Guid LegalEntityId,
    Guid ProductId,
    string FirstName,
    string LastName,
    Gender Gender,
    DateOnly? DateOfBirth,
    string? PersonalEmail,
    string? PersonalPhone,
    string? CurrentAddress,
    string? PermanentAddress,
    string? EmergencyContactName,
    string? EmergencyContactRelation,
    string? EmergencyContactPhone,
    string? BankAccountNumber,
    string? BankIfscCode,
    DateOnly DateOfJoining,
    string? Designation,
    string? Grade,
    string? Department,
    string? WorkLocation,
    Guid? ReportingManagerId,
    EmploymentType EmploymentType,
    decimal? MonthlyGrossSalary,
    DateOfBirthProofType? DateOfBirthProofType,
    string? ProfessionalTaxState,
    DateOnly? ProbationEndDate,
    DateOnly? ContractStartDate,
    DateOnly? ContractEndDate);

public record UpdateEmployeeStatusRequest(EmployeeStatus Status);

public record ConfirmEmployeeRequest(Guid ConfirmingManagerId, DateOnly? ConfirmationDate);

/// <summary>Wrapped so "employee not found" (null) is distinguishable from "found, but no
/// bank account on file" (BankAccountNumber is null but the DTO itself is not).</summary>
public record BankAccountRevealDto(string? BankAccountNumber);

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

/// <summary>Section 11: combinable filters shared by the list endpoint and the Excel export
/// (export ignores Page/PageSize and returns every matching row).</summary>
public record EmployeeListFilter(
    int Page,
    int PageSize,
    string? Search,
    EmployeeStatus? Status,
    Guid? LegalEntityId,
    Guid? ProductId,
    string? Department,
    string? Designation,
    string? WorkLocation);

/// <summary>Dashboard headline numbers - respects HRBP scope the same as everything else in this
/// service, so a scoped HRBP sees counts for their assigned legal entity/product only.</summary>
public record DashboardSummaryDto(
    int TotalEmployees,
    int ActiveEmployees,
    int DepartmentCount,
    IReadOnlyList<RecentJoineeDto> RecentJoinees);

public record RecentJoineeDto(
    Guid Id,
    string EmployeeCode,
    string FirstName,
    string LastName,
    string? Designation,
    string? Department,
    DateOnly DateOfJoining);
