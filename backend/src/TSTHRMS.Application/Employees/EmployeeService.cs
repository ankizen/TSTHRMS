using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Application.Employees.Dtos;
using TSTHRMS.Domain.Auditing;
using TSTHRMS.Domain.Employees;

namespace TSTHRMS.Application.Employees;

/// <summary>
/// Depends only on Application-layer interfaces (IApplicationDbContext, ISequenceGenerator,
/// ICurrentUserService) - no EF Core provider or ASP.NET Core reference, so this stays testable
/// without a database and portable if the persistence provider ever changes.
/// </summary>
public class EmployeeService(
    IApplicationDbContext dbContext,
    ISequenceGenerator sequenceGenerator,
    ICurrentUserService currentUserService) : IEmployeeService
{
    public async Task<PagedResult<EmployeeListItemDto>> GetListAsync(
        int page, int pageSize, string? search, EmployeeStatus? status, CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 50 : pageSize;

        var query = dbContext.Employees.AsNoTracking();

        if (status is not null)
        {
            query = query.Where(e => e.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(e =>
                EF.Functions.Like(e.FirstName, $"%{term}%") ||
                EF.Functions.Like(e.LastName, $"%{term}%") ||
                EF.Functions.Like(e.EmployeeCode, $"%{term}%"));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EmployeeListItemDto(
                e.Id, e.EmployeeCode, e.FirstName, e.LastName,
                e.LegalEntity!.Name, e.Product!.Name, e.Department, e.Designation, e.Status))
            .ToListAsync(cancellationToken);

        return new PagedResult<EmployeeListItemDto>(items, totalCount, page, pageSize);
    }

    public async Task<EmployeeDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees
            .Include(e => e.LegalEntity)
            .Include(e => e.Product)
            .Include(e => e.ReportingManager)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        return employee is null ? null : await ToDtoAsync(employee, cancellationToken);
    }

    public async Task<EmployeeDto> CreateAsync(EmployeeWriteRequest request, CancellationToken cancellationToken = default)
    {
        var nextValue = await sequenceGenerator.NextAsync("EmployeeCode", cancellationToken);

        var employee = new Employee
        {
            EmployeeCode = $"EMP{nextValue:D6}",
            LegalEntityId = request.LegalEntityId,
            ProductId = request.ProductId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Gender = request.Gender,
            DateOfBirth = request.DateOfBirth,
            PersonalEmail = request.PersonalEmail,
            PersonalPhone = request.PersonalPhone,
            CurrentAddress = request.CurrentAddress,
            PermanentAddress = request.PermanentAddress,
            EmergencyContactName = request.EmergencyContactName,
            EmergencyContactRelation = request.EmergencyContactRelation,
            EmergencyContactPhone = request.EmergencyContactPhone,
            BankAccountNumber = request.BankAccountNumber,
            BankIfscCode = request.BankIfscCode,
            DateOfJoining = request.DateOfJoining,
            Designation = request.Designation,
            Grade = request.Grade,
            Department = request.Department,
            ReportingManagerId = request.ReportingManagerId,
            EmploymentType = request.EmploymentType,
            MonthlyGrossSalary = request.MonthlyGrossSalary,
            DateOfBirthProofType = request.DateOfBirthProofType,
            ProfessionalTaxState = request.ProfessionalTaxState
        };

        dbContext.Employees.Add(employee);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(employee.Id, cancellationToken))!;
    }

    public async Task<EmployeeDto?> UpdateAsync(Guid id, EmployeeWriteRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (employee is null)
        {
            return null;
        }

        employee.LegalEntityId = request.LegalEntityId;
        employee.ProductId = request.ProductId;
        employee.FirstName = request.FirstName;
        employee.LastName = request.LastName;
        employee.Gender = request.Gender;
        employee.DateOfBirth = request.DateOfBirth;
        employee.PersonalEmail = request.PersonalEmail;
        employee.PersonalPhone = request.PersonalPhone;
        employee.CurrentAddress = request.CurrentAddress;
        employee.PermanentAddress = request.PermanentAddress;
        employee.EmergencyContactName = request.EmergencyContactName;
        employee.EmergencyContactRelation = request.EmergencyContactRelation;
        employee.EmergencyContactPhone = request.EmergencyContactPhone;

        // The UI shows this field masked/blank on edit (the real value is only ever fetched via
        // the audited reveal action), so a blank submission means "unchanged", not "clear it" -
        // otherwise every unrelated edit would silently wipe the bank account on save.
        if (!string.IsNullOrWhiteSpace(request.BankAccountNumber))
        {
            employee.BankAccountNumber = request.BankAccountNumber;
        }

        employee.BankIfscCode = request.BankIfscCode;
        employee.DateOfJoining = request.DateOfJoining;
        employee.Designation = request.Designation;
        employee.Grade = request.Grade;
        employee.Department = request.Department;
        employee.ReportingManagerId = request.ReportingManagerId;
        employee.EmploymentType = request.EmploymentType;
        employee.MonthlyGrossSalary = request.MonthlyGrossSalary;
        employee.DateOfBirthProofType = request.DateOfBirthProofType;
        employee.ProfessionalTaxState = request.ProfessionalTaxState;

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<EmployeeDto?> AcknowledgePoshPolicyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (employee is null)
        {
            return null;
        }

        employee.PoshAcknowledgedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<EmployeeDto?> UpdateStatusAsync(Guid id, EmployeeStatus status, CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (employee is null)
        {
            return null;
        }

        employee.Status = status;
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<BankAccountRevealDto?> RevealBankAccountNumberAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (employee is null)
        {
            return null;
        }

        dbContext.AuditLogs.Add(new AuditLog
        {
            TenantId = employee.TenantId,
            EntityName = nameof(Employee),
            EntityId = employee.Id.ToString(),
            Action = AuditAction.Revealed,
            ChangedByUserId = currentUserService.UserId,
            ChangedAt = DateTimeOffset.UtcNow,
            ChangesJson = JsonSerializer.Serialize(new[]
            {
                new AuditFieldChange(nameof(Employee.BankAccountNumber), null, null, true)
            })
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new BankAccountRevealDto(employee.BankAccountNumber);
    }

    private async Task<EmployeeDto> ToDtoAsync(Employee e, CancellationToken cancellationToken)
    {
        var eighteenYearsAgo = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-18);

        // Computed from Section 4 family data rather than stored redundantly - see Section 7's
        // note that this flag "affects gratuity nomination" without needing its own input.
        var hasMinorOrDifferentlyAbledDependent = await dbContext.FamilyMembers
            .Where(f => f.EmployeeId == e.Id && f.IsDependent)
            .AnyAsync(f => f.IsDifferentlyAbled || (f.DateOfBirth != null && f.DateOfBirth > eighteenYearsAgo), cancellationToken);

        return new EmployeeDto(
            e.Id,
            e.EmployeeCode,
            e.LegalEntityId,
            e.LegalEntity?.Name ?? string.Empty,
            e.ProductId,
            e.Product?.Name ?? string.Empty,
            e.Status,
            e.FirstName,
            e.LastName,
            e.Gender,
            e.DateOfBirth,
            e.PersonalEmail,
            e.PersonalPhone,
            e.CurrentAddress,
            e.PermanentAddress,
            e.EmergencyContactName,
            e.EmergencyContactRelation,
            e.EmergencyContactPhone,
            Masking.MaskLastFour(e.BankAccountNumber),
            e.BankIfscCode,
            e.DateOfJoining,
            e.Designation,
            e.Grade,
            e.Department,
            e.ReportingManagerId,
            e.ReportingManager is null ? null : $"{e.ReportingManager.FirstName} {e.ReportingManager.LastName}",
            e.EmploymentType,
            e.MonthlyGrossSalary,
            e.DateOfBirthProofType,
            e.ProfessionalTaxState,
            e.PoshAcknowledgedAt,
            ComplianceRules.IsPfApplicable(e.LegalEntity?.IsPfRegistered ?? false, e.MonthlyGrossSalary),
            ComplianceRules.IsEsicApplicable(e.LegalEntity?.IsEsicRegistered ?? false, e.MonthlyGrossSalary),
            ComplianceRules.IsMaharashtraLwfEligible(e.ProfessionalTaxState, e.MonthlyGrossSalary),
            hasMinorOrDifferentlyAbledDependent);
    }
}
