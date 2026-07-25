using Microsoft.EntityFrameworkCore;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Application.Employees.Dtos;
using TSTHRMS.Domain.Employees;

namespace TSTHRMS.Application.Employees;

public class EmployeeEditRequestService(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IUserDirectory userDirectory) : IEmployeeEditRequestService
{
    public async Task<IReadOnlyList<EmployeeEditRequestDto>> SubmitAsync(
        SubmitEditRequestsRequest request, CancellationToken cancellationToken = default)
    {
        if (currentUserService.EmployeeId is not { } employeeId)
        {
            return [];
        }

        var employee = await dbContext.Employees.FirstOrDefaultAsync(e => e.Id == employeeId, cancellationToken);
        if (employee is null)
        {
            return [];
        }

        var created = request.Changes.Select(change => new EmployeeEditRequest
        {
            EmployeeId = employeeId,
            Employee = employee,
            Field = change.Field,
            OldValue = GetCurrentValue(employee, change.Field),
            NewValue = change.NewValue ?? string.Empty
        }).ToList();

        dbContext.EmployeeEditRequests.AddRange(created);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await ToDtosAsync(created, cancellationToken);
    }

    public async Task<IReadOnlyList<EmployeeEditRequestDto>> GetOwnRequestsAsync(CancellationToken cancellationToken = default)
    {
        if (currentUserService.EmployeeId is not { } employeeId)
        {
            return [];
        }

        var requests = await dbContext.EmployeeEditRequests
            .AsNoTracking()
            .Include(r => r.Employee)
            .Where(r => r.EmployeeId == employeeId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        return await ToDtosAsync(requests, cancellationToken);
    }

    public async Task<IReadOnlyList<EmployeeEditRequestDto>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        var requests = await dbContext.EmployeeEditRequests
            .AsNoTracking()
            .Include(r => r.Employee)
            .Where(r => r.Status == EditRequestStatus.Pending)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        var inScope = requests.Where(r => r.Employee is not null && !IsHrbpOutOfScope(r.Employee.LegalEntityId, r.Employee.ProductId)).ToList();

        return await ToDtosAsync(inScope, cancellationToken);
    }

    public async Task<EmployeeEditRequestDto?> ApproveAsync(
        Guid requestId, ReviewEditRequestDto request, CancellationToken cancellationToken = default)
    {
        var editRequest = await dbContext.EmployeeEditRequests
            .Include(r => r.Employee)
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);

        if (editRequest?.Employee is null
            || editRequest.Status != EditRequestStatus.Pending
            || IsHrbpOutOfScope(editRequest.Employee.LegalEntityId, editRequest.Employee.ProductId))
        {
            return null;
        }

        ApplyField(editRequest.Employee, editRequest.Field, editRequest.NewValue);
        editRequest.Status = EditRequestStatus.Approved;
        editRequest.ReviewedByUserId = currentUserService.UserId;
        editRequest.ReviewedAt = DateTimeOffset.UtcNow;
        editRequest.ReviewNote = request.ReviewNote;

        await dbContext.SaveChangesAsync(cancellationToken);

        return (await ToDtosAsync([editRequest], cancellationToken))[0];
    }

    public async Task<EmployeeEditRequestDto?> RejectAsync(
        Guid requestId, ReviewEditRequestDto request, CancellationToken cancellationToken = default)
    {
        var editRequest = await dbContext.EmployeeEditRequests
            .Include(r => r.Employee)
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);

        if (editRequest?.Employee is null
            || editRequest.Status != EditRequestStatus.Pending
            || IsHrbpOutOfScope(editRequest.Employee.LegalEntityId, editRequest.Employee.ProductId))
        {
            return null;
        }

        editRequest.Status = EditRequestStatus.Rejected;
        editRequest.ReviewedByUserId = currentUserService.UserId;
        editRequest.ReviewedAt = DateTimeOffset.UtcNow;
        editRequest.ReviewNote = request.ReviewNote;

        await dbContext.SaveChangesAsync(cancellationToken);

        return (await ToDtosAsync([editRequest], cancellationToken))[0];
    }

    /// <summary>Mirrors EmployeeService's own scope check - kept local rather than shared since
    /// pulling it out into a common helper isn't worth an abstraction for four lines used twice.</summary>
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

    private static string? GetCurrentValue(Employee employee, EditableEmployeeField field) => field switch
    {
        EditableEmployeeField.PersonalEmail => employee.PersonalEmail,
        EditableEmployeeField.PersonalPhone => employee.PersonalPhone,
        EditableEmployeeField.CurrentAddress => employee.CurrentAddress,
        EditableEmployeeField.PermanentAddress => employee.PermanentAddress,
        EditableEmployeeField.EmergencyContactName => employee.EmergencyContactName,
        EditableEmployeeField.EmergencyContactRelation => employee.EmergencyContactRelation,
        EditableEmployeeField.EmergencyContactPhone => employee.EmergencyContactPhone,
        _ => throw new ArgumentOutOfRangeException(nameof(field))
    };

    private static void ApplyField(Employee employee, EditableEmployeeField field, string newValue)
    {
        switch (field)
        {
            case EditableEmployeeField.PersonalEmail:
                employee.PersonalEmail = newValue;
                break;
            case EditableEmployeeField.PersonalPhone:
                employee.PersonalPhone = newValue;
                break;
            case EditableEmployeeField.CurrentAddress:
                employee.CurrentAddress = newValue;
                break;
            case EditableEmployeeField.PermanentAddress:
                employee.PermanentAddress = newValue;
                break;
            case EditableEmployeeField.EmergencyContactName:
                employee.EmergencyContactName = newValue;
                break;
            case EditableEmployeeField.EmergencyContactRelation:
                employee.EmergencyContactRelation = newValue;
                break;
            case EditableEmployeeField.EmergencyContactPhone:
                employee.EmergencyContactPhone = newValue;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(field));
        }
    }

    private async Task<List<EmployeeEditRequestDto>> ToDtosAsync(
        IReadOnlyList<EmployeeEditRequest> requests, CancellationToken cancellationToken)
    {
        var userIds = requests
            .Where(r => r.ReviewedByUserId is not null)
            .Select(r => r.ReviewedByUserId!.Value)
            .Distinct()
            .ToList();
        var displayNames = await userDirectory.GetDisplayNamesAsync(userIds, cancellationToken);

        return requests.Select(r => new EmployeeEditRequestDto(
            r.Id,
            r.EmployeeId,
            r.Employee is null ? string.Empty : $"{r.Employee.FirstName} {r.Employee.LastName}",
            r.Field,
            r.OldValue,
            r.NewValue,
            r.Status,
            r.ReviewedByUserId,
            r.ReviewedByUserId is not null && displayNames.TryGetValue(r.ReviewedByUserId.Value, out var name) ? name : null,
            r.ReviewedAt,
            r.ReviewNote,
            r.CreatedAt)).ToList();
    }
}
