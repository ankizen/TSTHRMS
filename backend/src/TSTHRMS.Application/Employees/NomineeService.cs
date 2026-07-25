using Microsoft.EntityFrameworkCore;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Common.Dtos;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Application.Employees.Dtos;
using TSTHRMS.Domain.Employees;

namespace TSTHRMS.Application.Employees;

public class NomineeService(
    IApplicationDbContext dbContext,
    IFileStorageService fileStorageService,
    ICurrentUserService currentUserService) : INomineeService
{
    public async Task<IReadOnlyList<NomineeDto>> GetForEmployeeAsync(
        Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Nominees
            .Include(n => n.FamilyMember)
            .Include(n => n.ConsentDocument)
            .AsNoTracking()
            .Where(n => n.EmployeeId == employeeId)
            .OrderBy(n => n.NominationType).ThenBy(n => n.Name)
            .Select(n => ToDto(n))
            .ToListAsync(cancellationToken);
    }

    public async Task<NomineeUpsertResult?> CreateAsync(
        Guid employeeId, NomineeWriteRequest request, CancellationToken cancellationToken = default)
    {
        var employeeExists = await dbContext.Employees.AnyAsync(e => e.Id == employeeId, cancellationToken);
        if (!employeeExists)
        {
            return null;
        }

        if (!await FamilyMemberBelongsToEmployeeAsync(employeeId, request.FamilyMemberId, cancellationToken))
        {
            return NomineeUpsertResult.Failure("Selected family member does not belong to this employee.");
        }

        var shareError = await ValidateShareAsync(employeeId, request.NominationType, request.SharePercentage, null, cancellationToken);
        if (shareError is not null)
        {
            return NomineeUpsertResult.Failure(shareError);
        }

        var nominee = new Nominee
        {
            EmployeeId = employeeId,
            NominationType = request.NominationType,
            Name = request.Name,
            Relation = request.Relation,
            SharePercentage = request.SharePercentage,
            ContactNumber = request.ContactNumber,
            FamilyMemberId = request.FamilyMemberId
        };

        dbContext.Nominees.Add(nominee);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NomineeUpsertResult.Success(await ToDtoAsync(nominee, cancellationToken));
    }

    public async Task<NomineeUpsertResult?> UpdateAsync(
        Guid employeeId, Guid id, NomineeWriteRequest request, CancellationToken cancellationToken = default)
    {
        var nominee = await FindAsync(employeeId, id, cancellationToken);
        if (nominee is null)
        {
            return null;
        }

        if (!await FamilyMemberBelongsToEmployeeAsync(employeeId, request.FamilyMemberId, cancellationToken))
        {
            return NomineeUpsertResult.Failure("Selected family member does not belong to this employee.");
        }

        var shareError = await ValidateShareAsync(employeeId, request.NominationType, request.SharePercentage, id, cancellationToken);
        if (shareError is not null)
        {
            return NomineeUpsertResult.Failure(shareError);
        }

        nominee.NominationType = request.NominationType;
        nominee.Name = request.Name;
        nominee.Relation = request.Relation;
        nominee.SharePercentage = request.SharePercentage;
        nominee.ContactNumber = request.ContactNumber;
        nominee.FamilyMemberId = request.FamilyMemberId;

        await dbContext.SaveChangesAsync(cancellationToken);

        return NomineeUpsertResult.Success(await ToDtoAsync(nominee, cancellationToken));
    }

    public async Task<bool> DeleteAsync(Guid employeeId, Guid id, CancellationToken cancellationToken = default)
    {
        var nominee = await FindAsync(employeeId, id, cancellationToken);
        if (nominee is null)
        {
            return false;
        }

        nominee.IsDeleted = true;
        nominee.DeletedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<AttachDocumentResult<NomineeDto>?> AttachConsentDocumentAsync(
        Guid employeeId, Guid id, Stream content, string fileName, string contentType, long sizeBytes,
        CancellationToken cancellationToken = default)
    {
        var nominee = await FindAsync(employeeId, id, cancellationToken);
        if (nominee is null)
        {
            return null;
        }

        var validationError = DocumentValidation.Validate(sizeBytes, contentType);
        if (validationError is not null)
        {
            return AttachDocumentResult<NomineeDto>.Failure(validationError);
        }

        nominee.ConsentDocument = await DocumentAttachmentHelper.SaveAndReplaceAsync(
            dbContext, fileStorageService, nominee.TenantId, nominee.ConsentDocumentId,
            content, fileName, contentType, sizeBytes, currentUserService.UserId, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return AttachDocumentResult<NomineeDto>.Success(await ToDtoAsync(nominee, cancellationToken));
    }

    private async Task<bool> FamilyMemberBelongsToEmployeeAsync(
        Guid employeeId, Guid? familyMemberId, CancellationToken cancellationToken)
    {
        if (familyMemberId is null)
        {
            return true;
        }

        return await dbContext.FamilyMembers
            .AnyAsync(f => f.Id == familyMemberId && f.EmployeeId == employeeId, cancellationToken);
    }

    private async Task<string?> ValidateShareAsync(
        Guid employeeId, NominationType type, decimal? share, Guid? excludeId, CancellationToken cancellationToken)
    {
        if (share is null || type == NominationType.Insurance)
        {
            return null;
        }

        var existingTotal = await dbContext.Nominees
            .Where(n => n.EmployeeId == employeeId && n.NominationType == type && n.Id != (excludeId ?? Guid.Empty))
            .SumAsync(n => n.SharePercentage ?? 0, cancellationToken);

        var newTotal = existingTotal + share.Value;
        return newTotal > 100
            ? $"Total share for {type} nominees would be {newTotal}%, which exceeds 100%."
            : null;
    }

    private Task<Nominee?> FindAsync(Guid employeeId, Guid id, CancellationToken cancellationToken) =>
        dbContext.Nominees
            .Include(n => n.FamilyMember)
            .Include(n => n.ConsentDocument)
            .FirstOrDefaultAsync(n => n.Id == id && n.EmployeeId == employeeId, cancellationToken);

    private async Task<NomineeDto> ToDtoAsync(Nominee nominee, CancellationToken cancellationToken)
    {
        // Create/Update mutate the tracked entity directly rather than re-querying, so the
        // FamilyMember/ConsentDocument navigations may still be unloaded - load them once here.
        if (nominee.FamilyMemberId is not null && nominee.FamilyMember is null)
        {
            nominee.FamilyMember = await dbContext.FamilyMembers.FindAsync([nominee.FamilyMemberId], cancellationToken);
        }

        if (nominee.ConsentDocumentId is not null && nominee.ConsentDocument is null)
        {
            nominee.ConsentDocument = await dbContext.Documents.FindAsync([nominee.ConsentDocumentId], cancellationToken);
        }

        return ToDto(nominee);
    }

    private static NomineeDto ToDto(Nominee nominee) => new(
        nominee.Id,
        nominee.EmployeeId,
        nominee.NominationType,
        nominee.Name,
        nominee.Relation,
        nominee.SharePercentage,
        nominee.ContactNumber,
        nominee.FamilyMemberId,
        nominee.FamilyMember?.Name,
        nominee.ConsentDocumentId,
        nominee.ConsentDocument?.FileName);
}
