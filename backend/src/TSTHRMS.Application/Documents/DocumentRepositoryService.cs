using Microsoft.EntityFrameworkCore;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Common.Dtos;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Application.Documents.Dtos;
using TSTHRMS.Domain.Documents;

namespace TSTHRMS.Application.Documents;

public class DocumentRepositoryService(
    IApplicationDbContext dbContext,
    IFileStorageService fileStorageService,
    ICurrentUserService currentUserService) : IDocumentRepositoryService
{
    public async Task<IReadOnlyList<DocumentSummaryDto>> GetForEmployeeAsync(
        Guid employeeId, CancellationToken cancellationToken = default)
    {
        var results = new List<DocumentSummaryDto>();

        var standalone = await dbContext.EmployeeDocuments
            .Include(d => d.Document)
            .AsNoTracking()
            .Where(d => d.EmployeeId == employeeId)
            .Select(d => new DocumentSummaryDto(
                d.DocumentId, d.Document!.FileName, CategoryLabel(d.Category), d.Notes, d.Document.UploadedAt, d.Id))
            .ToListAsync(cancellationToken);
        results.AddRange(standalone);

        var educationDocs = await dbContext.EducationRecords
            .Include(e => e.CertificateDocument)
            .AsNoTracking()
            .Where(e => e.EmployeeId == employeeId && e.CertificateDocumentId != null)
            .Select(e => new DocumentSummaryDto(
                e.CertificateDocumentId!.Value, e.CertificateDocument!.FileName, "Education Certificate",
                e.DegreeName, e.CertificateDocument.UploadedAt, null))
            .ToListAsync(cancellationToken);
        results.AddRange(educationDocs);

        var previousEmployment = await dbContext.PreviousEmploymentRecords
            .Include(p => p.RelievingLetterDocument)
            .Include(p => p.LastSalarySlipDocument)
            .AsNoTracking()
            .Where(p => p.EmployeeId == employeeId)
            .ToListAsync(cancellationToken);
        foreach (var p in previousEmployment)
        {
            if (p.RelievingLetterDocument is not null)
            {
                results.Add(new DocumentSummaryDto(
                    p.RelievingLetterDocument.Id, p.RelievingLetterDocument.FileName, "Relieving Letter",
                    p.CompanyName, p.RelievingLetterDocument.UploadedAt, null));
            }

            if (p.LastSalarySlipDocument is not null)
            {
                results.Add(new DocumentSummaryDto(
                    p.LastSalarySlipDocument.Id, p.LastSalarySlipDocument.FileName, "Salary Slip",
                    p.CompanyName, p.LastSalarySlipDocument.UploadedAt, null));
            }
        }

        var identityDocs = await dbContext.IdentityDocuments
            .Include(d => d.ProofDocument)
            .AsNoTracking()
            .Where(d => d.EmployeeId == employeeId && d.ProofDocumentId != null)
            .ToListAsync(cancellationToken);
        foreach (var d in identityDocs)
        {
            results.Add(new DocumentSummaryDto(
                d.ProofDocument!.Id, d.ProofDocument.FileName, $"Identity Proof - {d.DocumentType}",
                null, d.ProofDocument.UploadedAt, null));
        }

        var nomineeDocs = await dbContext.Nominees
            .Include(n => n.ConsentDocument)
            .AsNoTracking()
            .Where(n => n.EmployeeId == employeeId && n.ConsentDocumentId != null)
            .ToListAsync(cancellationToken);
        foreach (var n in nomineeDocs)
        {
            results.Add(new DocumentSummaryDto(
                n.ConsentDocument!.Id, n.ConsentDocument.FileName, "Nominee Consent",
                n.Name, n.ConsentDocument.UploadedAt, null));
        }

        return results.OrderByDescending(r => r.UploadedAt).ToList();
    }

    public async Task<AttachDocumentResult<DocumentSummaryDto>?> UploadAsync(
        Guid employeeId, EmployeeDocumentWriteRequest request,
        Stream content, string fileName, string contentType, long sizeBytes,
        CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees.FirstOrDefaultAsync(e => e.Id == employeeId, cancellationToken);
        if (employee is null)
        {
            return null;
        }

        var validationError = DocumentValidation.Validate(sizeBytes, contentType);
        if (validationError is not null)
        {
            return AttachDocumentResult<DocumentSummaryDto>.Failure(validationError);
        }

        var document = await DocumentAttachmentHelper.SaveAndReplaceAsync(
            dbContext, fileStorageService, employee.TenantId, null,
            content, fileName, contentType, sizeBytes, currentUserService.UserId, cancellationToken);

        var employeeDocument = new EmployeeDocument
        {
            EmployeeId = employeeId,
            Category = request.Category,
            DocumentId = document.Id,
            Document = document,
            Notes = request.Notes
        };

        dbContext.EmployeeDocuments.Add(employeeDocument);
        await dbContext.SaveChangesAsync(cancellationToken);

        return AttachDocumentResult<DocumentSummaryDto>.Success(new DocumentSummaryDto(
            document.Id, document.FileName, CategoryLabel(employeeDocument.Category), employeeDocument.Notes,
            document.UploadedAt, employeeDocument.Id));
    }

    public async Task<bool> DeleteAsync(Guid employeeId, Guid employeeDocumentId, CancellationToken cancellationToken = default)
    {
        var employeeDocument = await dbContext.EmployeeDocuments
            .Include(d => d.Document)
            .FirstOrDefaultAsync(d => d.Id == employeeDocumentId && d.EmployeeId == employeeId, cancellationToken);
        if (employeeDocument is null)
        {
            return false;
        }

        if (employeeDocument.Document is not null)
        {
            await fileStorageService.DeleteAsync(employeeDocument.Document.StorageKey, cancellationToken);
            dbContext.Documents.Remove(employeeDocument.Document);
        }

        dbContext.EmployeeDocuments.Remove(employeeDocument);
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static string CategoryLabel(EmployeeDocumentCategory category) => category switch
    {
        EmployeeDocumentCategory.OfferLetter => "Offer Letter",
        EmployeeDocumentCategory.PolicyAcknowledgement => "Policy Acknowledgement",
        EmployeeDocumentCategory.Other => "Other",
        _ => category.ToString()
    };
}
