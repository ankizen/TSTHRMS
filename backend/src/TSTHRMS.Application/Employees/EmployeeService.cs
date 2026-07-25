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
        EmployeeListFilter filter, CancellationToken cancellationToken = default)
    {
        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize is < 1 or > 200 ? 50 : filter.PageSize;

        var query = ApplyHrbpScope(ApplyFilter(dbContext.Employees.AsNoTracking(), filter));

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EmployeeListItemDto(
                e.Id, e.EmployeeCode, e.FirstName, e.LastName,
                e.LegalEntity!.Name, e.Product!.Name, e.Department, e.Designation, e.WorkLocation, e.Status))
            .ToListAsync(cancellationToken);

        return new PagedResult<EmployeeListItemDto>(items, totalCount, page, pageSize);
    }

    public async Task<byte[]> ExportToExcelAsync(EmployeeListFilter filter, CancellationToken cancellationToken = default)
    {
        var query = ApplyHrbpScope(ApplyFilter(dbContext.Employees.AsNoTracking(), filter));

        var rows = await query
            .OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
            .Select(e => new EmployeeListItemDto(
                e.Id, e.EmployeeCode, e.FirstName, e.LastName,
                e.LegalEntity!.Name, e.Product!.Name, e.Department, e.Designation, e.WorkLocation, e.Status))
            .ToListAsync(cancellationToken);

        return EmployeeExcelExporter.Export(rows);
    }

    /// <summary>Shared by the paged list and the (unpaged) Excel export so the two can never
    /// silently drift apart on what "matches the current filter" means.</summary>
    private static IQueryable<Employee> ApplyFilter(IQueryable<Employee> query, EmployeeListFilter filter)
    {
        if (filter.Status is not null)
        {
            query = query.Where(e => e.Status == filter.Status);
        }

        if (filter.LegalEntityId is not null)
        {
            query = query.Where(e => e.LegalEntityId == filter.LegalEntityId);
        }

        if (filter.ProductId is not null)
        {
            query = query.Where(e => e.ProductId == filter.ProductId);
        }

        // Partial-match (not equality) since these are free-text fields typed into a filter box,
        // not selected from a fixed list - the same reasoning as WorkLocation's own field comment.
        if (!string.IsNullOrWhiteSpace(filter.Department))
        {
            var term = filter.Department.Trim();
            query = query.Where(e => e.Department != null && EF.Functions.Like(e.Department, $"%{term}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.Designation))
        {
            var term = filter.Designation.Trim();
            query = query.Where(e => e.Designation != null && EF.Functions.Like(e.Designation, $"%{term}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.WorkLocation))
        {
            var term = filter.WorkLocation.Trim();
            query = query.Where(e => e.WorkLocation != null && EF.Functions.Like(e.WorkLocation, $"%{term}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(e =>
                EF.Functions.Like(e.FirstName, $"%{term}%") ||
                EF.Functions.Like(e.LastName, $"%{term}%") ||
                EF.Functions.Like(e.EmployeeCode, $"%{term}%") ||
                (e.PersonalEmail != null && EF.Functions.Like(e.PersonalEmail, $"%{term}%")));
        }

        return query;
    }

    /// <summary>An HRBP scoped to a legal entity and/or product only ever sees rows within that
    /// scope - applied on top of whatever filter the caller explicitly asked for, so a scoped
    /// HRBP requesting data outside their scope just gets zero rows rather than an error.
    /// HRAdmin (and any caller without the HRBP role) is unaffected.</summary>
    private IQueryable<Employee> ApplyHrbpScope(IQueryable<Employee> query)
    {
        if (!currentUserService.Roles.Contains(RoleNames.HRBP) || currentUserService.Roles.Contains(RoleNames.HRAdmin))
        {
            return query;
        }

        if (currentUserService.AssignedLegalEntityId is { } scopedEntity)
        {
            query = query.Where(e => e.LegalEntityId == scopedEntity);
        }

        if (currentUserService.AssignedProductId is { } scopedProduct)
        {
            query = query.Where(e => e.ProductId == scopedProduct);
        }

        return query;
    }

    /// <summary>True when the caller is an HRBP whose assigned legal entity/product scope
    /// excludes the given combination - used to block reads/writes that ApplyHrbpScope's
    /// query-level filtering can't reach (e.g. GetById, Create, Update).</summary>
    private bool IsHrbpOutOfScope(Guid legalEntityId, Guid productId)
    {
        if (!currentUserService.Roles.Contains(RoleNames.HRBP) || currentUserService.Roles.Contains(RoleNames.HRAdmin))
        {
            return false;
        }

        if (currentUserService.AssignedLegalEntityId is { } scopedEntity && scopedEntity != legalEntityId)
        {
            return true;
        }

        return currentUserService.AssignedProductId is { } scopedProduct && scopedProduct != productId;
    }

    public async Task<IReadOnlyList<OrgChartNodeDto>> GetOrgChartAsync(
        Guid? legalEntityId, Guid? productId, CancellationToken cancellationToken = default)
    {
        // Exited employees are dropped from the chart; a still-active employee whose manager
        // has exited (or fell outside the entity/product filter) just renders as a root node -
        // simpler than trying to walk past a manager the chart can't show.
        var query = ApplyHrbpScope(dbContext.Employees.AsNoTracking().Where(e => e.Status != EmployeeStatus.Exited));

        if (legalEntityId is not null)
        {
            query = query.Where(e => e.LegalEntityId == legalEntityId);
        }

        if (productId is not null)
        {
            query = query.Where(e => e.ProductId == productId);
        }

        return await query
            .OrderBy(e => e.FirstName).ThenBy(e => e.LastName)
            .Select(e => new OrgChartNodeDto(
                e.Id, e.FirstName + " " + e.LastName, e.Designation, e.Department, e.ReportingManagerId, e.Status))
            .ToListAsync(cancellationToken);
    }

    public async Task<EmployeeDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees
            .Include(e => e.LegalEntity)
            .Include(e => e.Product)
            .Include(e => e.ReportingManager)
            .Include(e => e.ConfirmingManager)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (employee is null || IsHrbpOutOfScope(employee.LegalEntityId, employee.ProductId))
        {
            return null;
        }

        return await ToDtoAsync(employee, cancellationToken);
    }

    /// <summary>Null return means the employee wasn't created - either because the request itself
    /// asked for a legal entity/product outside a scoped HRBP's assignment (the only failure mode
    /// today, since request-shape validation already ran before this is called).</summary>
    public async Task<EmployeeDto?> CreateAsync(EmployeeWriteRequest request, CancellationToken cancellationToken = default)
    {
        if (IsHrbpOutOfScope(request.LegalEntityId, request.ProductId))
        {
            return null;
        }

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
            WorkLocation = request.WorkLocation,
            ReportingManagerId = request.ReportingManagerId,
            EmploymentType = request.EmploymentType,
            MonthlyGrossSalary = request.MonthlyGrossSalary,
            DateOfBirthProofType = request.DateOfBirthProofType,
            ProfessionalTaxState = request.ProfessionalTaxState,
            ProbationEndDate = request.ProbationEndDate ?? request.DateOfJoining.AddMonths(ProbationDefaults.DurationMonths),
            ContractStartDate = request.ContractStartDate,
            ContractEndDate = request.ContractEndDate
        };

        dbContext.Employees.Add(employee);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(employee.Id, cancellationToken))!;
    }

    public async Task<EmployeeDto?> UpdateAsync(Guid id, EmployeeWriteRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (employee is null || IsHrbpOutOfScope(employee.LegalEntityId, employee.ProductId))
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
        employee.WorkLocation = request.WorkLocation;
        employee.ReportingManagerId = request.ReportingManagerId;
        employee.EmploymentType = request.EmploymentType;
        employee.MonthlyGrossSalary = request.MonthlyGrossSalary;
        employee.DateOfBirthProofType = request.DateOfBirthProofType;
        employee.ProfessionalTaxState = request.ProfessionalTaxState;
        employee.ProbationEndDate = request.ProbationEndDate ?? employee.ProbationEndDate;
        employee.ContractStartDate = request.ContractStartDate;
        employee.ContractEndDate = request.ContractEndDate;

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<EmployeeDto?> ConfirmAsync(Guid id, ConfirmEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (employee is null || IsHrbpOutOfScope(employee.LegalEntityId, employee.ProductId))
        {
            return null;
        }

        employee.ConfirmationStatus = ConfirmationStatus.Confirmed;
        employee.ConfirmationDate = request.ConfirmationDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        employee.ConfirmingManagerId = request.ConfirmingManagerId;

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<EmployeeDto?> AcknowledgePoshPolicyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (employee is null || IsHrbpOutOfScope(employee.LegalEntityId, employee.ProductId))
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
        if (employee is null || IsHrbpOutOfScope(employee.LegalEntityId, employee.ProductId))
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
        if (employee is null || IsHrbpOutOfScope(employee.LegalEntityId, employee.ProductId))
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
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var eighteenYearsAgo = today.AddYears(-18);

        var isContractExpiringSoon = e.ContractEndDate is not null
            && e.ContractEndDate >= today
            && e.ContractEndDate <= today.AddDays(ProbationDefaults.ContractExpiryWarningDays);

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
            e.WorkLocation,
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
            hasMinorOrDifferentlyAbledDependent,
            e.ProbationEndDate,
            e.ConfirmationStatus,
            e.ConfirmationDate,
            e.ConfirmingManagerId,
            e.ConfirmingManager is null ? null : $"{e.ConfirmingManager.FirstName} {e.ConfirmingManager.LastName}",
            e.ContractStartDate,
            e.ContractEndDate,
            isContractExpiringSoon);
    }
}
