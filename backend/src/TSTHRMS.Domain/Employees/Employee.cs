using TSTHRMS.Domain.Common;
using TSTHRMS.Domain.Tenancy;

namespace TSTHRMS.Domain.Employees;

/// <summary>
/// The Core HR master record. One row per employee - the single source of truth every other
/// module (Attendance, Leave, Payroll, ESS) reads from. Never hard-deleted: exits are modeled
/// by <see cref="EmployeeStatus.Exited"/>, not row removal.
/// </summary>
public class Employee : TenantScopedEntity
{
    /// <summary>Auto-generated via ISequenceGenerator, e.g. "EMP000001". Never reused, even after exit.</summary>
    public required string EmployeeCode { get; set; }

    public Guid LegalEntityId { get; set; }
    public LegalEntity? LegalEntity { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;

    // Personal
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public Gender Gender { get; set; }
    public DateOnly? DateOfBirth { get; set; }

    // Contact
    public string? PersonalEmail { get; set; }
    public string? PersonalPhone { get; set; }
    public string? CurrentAddress { get; set; }
    public string? PermanentAddress { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactRelation { get; set; }
    public string? EmergencyContactPhone { get; set; }

    // Statutory (salary disbursement) - PAN/Aadhaar/UAN/ESIC land here in slice 6/7
    [Sensitive]
    public string? BankAccountNumber { get; set; }
    public string? BankIfscCode { get; set; }

    // Employment
    public DateOnly DateOfJoining { get; set; }
    public string? Designation { get; set; }
    public string? Grade { get; set; }
    public string? Department { get; set; }
    public Guid? ReportingManagerId { get; set; }
    public Employee? ReportingManager { get; set; }
    public EmploymentType EmploymentType { get; set; }

    // Statutory & Compliance (Section 7) - PF/ESIC/LWF applicability are computed from these
    // plus LegalEntity registration flags, not stored as manually-toggled booleans.
    public decimal? MonthlyGrossSalary { get; set; }
    public DateOfBirthProofType? DateOfBirthProofType { get; set; }
    public string? ProfessionalTaxState { get; set; }
    public DateTimeOffset? PoshAcknowledgedAt { get; set; }

    // Probation & Contract Tracking (Section 8)
    public DateOnly? ProbationEndDate { get; set; }
    public ConfirmationStatus ConfirmationStatus { get; set; } = ConfirmationStatus.Probation;
    public DateOnly? ConfirmationDate { get; set; }
    public Guid? ConfirmingManagerId { get; set; }
    public Employee? ConfirmingManager { get; set; }

    /// <summary>Only meaningful for Contract/Intern employment types.</summary>
    public DateOnly? ContractStartDate { get; set; }
    public DateOnly? ContractEndDate { get; set; }
}

public enum Gender
{
    Male,
    Female,
    Other,
    PreferNotToSay
}

public enum EmploymentType
{
    FullTime,
    Contract,
    Intern
}

public enum EmployeeStatus
{
    Active,
    OnLeave,
    NoticePeriod,
    Exited
}

/// <summary>Which document was used to establish date of birth - required for PF and gratuity validity.</summary>
public enum DateOfBirthProofType
{
    Aadhaar,
    BirthCertificate,
    TenthMarksheet,
    Other
}

public enum ConfirmationStatus
{
    Probation,
    Confirmed
}
