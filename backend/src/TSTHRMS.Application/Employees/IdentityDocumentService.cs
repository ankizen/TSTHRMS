using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Common.Dtos;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Application.Employees.Dtos;
using TSTHRMS.Domain.Auditing;
using TSTHRMS.Domain.Employees;

namespace TSTHRMS.Application.Employees;

public class IdentityDocumentService(
    IApplicationDbContext dbContext,
    IFileStorageService fileStorageService,
    ICurrentUserService currentUserService) : IIdentityDocumentService
{
    public async Task<IReadOnlyList<IdentityDocumentDto>> GetForEmployeeAsync(
        Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await dbContext.IdentityDocuments
            .Include(d => d.ProofDocument)
            .AsNoTracking()
            .Where(d => d.EmployeeId == employeeId)
            .OrderBy(d => d.DocumentType)
            .Select(d => ToDto(d))
            .ToListAsync(cancellationToken);
    }

    public async Task<IdentityDocumentUpsertResult?> CreateAsync(
        Guid employeeId, IdentityDocumentWriteRequest request, CancellationToken cancellationToken = default)
    {
        var employeeExists = await dbContext.Employees.AnyAsync(e => e.Id == employeeId, cancellationToken);
        if (!employeeExists)
        {
            return null;
        }

        var alreadyExists = await dbContext.IdentityDocuments
            .AnyAsync(d => d.EmployeeId == employeeId && d.DocumentType == request.DocumentType, cancellationToken);
        if (alreadyExists)
        {
            return IdentityDocumentUpsertResult.Failure(
                $"A {request.DocumentType} record already exists for this employee - edit it instead.");
        }

        var document = new IdentityDocument
        {
            EmployeeId = employeeId,
            DocumentType = request.DocumentType,
            Number = request.Number,
            ExpiryDate = request.ExpiryDate
        };

        dbContext.IdentityDocuments.Add(document);
        await dbContext.SaveChangesAsync(cancellationToken);

        return IdentityDocumentUpsertResult.Success(ToDto(document));
    }

    public async Task<IdentityDocumentDto?> UpdateAsync(
        Guid employeeId, Guid id, IdentityDocumentWriteRequest request, CancellationToken cancellationToken = default)
    {
        var document = await FindAsync(employeeId, id, cancellationToken);
        if (document is null)
        {
            return null;
        }

        // DocumentType is immutable after creation - request.DocumentType only informed validation.
        document.Number = request.Number;
        document.ExpiryDate = request.ExpiryDate;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(document);
    }

    public async Task<bool> DeleteAsync(Guid employeeId, Guid id, CancellationToken cancellationToken = default)
    {
        var document = await FindAsync(employeeId, id, cancellationToken);
        if (document is null)
        {
            return false;
        }

        document.IsDeleted = true;
        document.DeletedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<AttachDocumentResult<IdentityDocumentDto>?> AttachProofAsync(
        Guid employeeId, Guid id, Stream content, string fileName, string contentType, long sizeBytes,
        CancellationToken cancellationToken = default)
    {
        var document = await FindAsync(employeeId, id, cancellationToken);
        if (document is null)
        {
            return null;
        }

        var validationError = DocumentValidation.Validate(sizeBytes, contentType);
        if (validationError is not null)
        {
            return AttachDocumentResult<IdentityDocumentDto>.Failure(validationError);
        }

        document.ProofDocument = await DocumentAttachmentHelper.SaveAndReplaceAsync(
            dbContext, fileStorageService, document.TenantId, document.ProofDocumentId,
            content, fileName, contentType, sizeBytes, currentUserService.UserId, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return AttachDocumentResult<IdentityDocumentDto>.Success(ToDto(document));
    }

    public async Task<IdentityNumberRevealDto?> RevealNumberAsync(
        Guid employeeId, Guid id, CancellationToken cancellationToken = default)
    {
        var document = await FindAsync(employeeId, id, cancellationToken);
        if (document is null)
        {
            return null;
        }

        dbContext.AuditLogs.Add(new AuditLog
        {
            TenantId = document.TenantId,
            EntityName = nameof(IdentityDocument),
            EntityId = document.Id.ToString(),
            Action = AuditAction.Revealed,
            ChangedByUserId = currentUserService.UserId,
            ChangedAt = DateTimeOffset.UtcNow,
            ChangesJson = JsonSerializer.Serialize(new[]
            {
                new AuditFieldChange(nameof(IdentityDocument.Number), null, null, true)
            })
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new IdentityNumberRevealDto(document.Number);
    }

    private Task<IdentityDocument?> FindAsync(Guid employeeId, Guid id, CancellationToken cancellationToken) =>
        dbContext.IdentityDocuments
            .Include(d => d.ProofDocument)
            .FirstOrDefaultAsync(d => d.Id == id && d.EmployeeId == employeeId, cancellationToken);

    private static IdentityDocumentDto ToDto(IdentityDocument document) => new(
        document.Id,
        document.EmployeeId,
        document.DocumentType,
        document.DocumentType == IdentityDocumentType.Aadhaar
            ? Masking.MaskLastFour(document.Number) ?? document.Number
            : document.Number,
        document.ExpiryDate,
        document.ProofDocumentId,
        document.ProofDocument?.FileName);
}
