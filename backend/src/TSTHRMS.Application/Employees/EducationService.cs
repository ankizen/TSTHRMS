using Microsoft.EntityFrameworkCore;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Application.Employees.Dtos;
using TSTHRMS.Domain.Documents;
using TSTHRMS.Domain.Employees;

namespace TSTHRMS.Application.Employees;

public class EducationService(IApplicationDbContext dbContext, IFileStorageService fileStorageService) : IEducationService
{
    private static readonly HashSet<string> AllowedContentTypes =
        new(StringComparer.OrdinalIgnoreCase) { "application/pdf", "image/jpeg", "image/png" };

    private const long MaxFileSizeBytes = 10 * 1024 * 1024;

    public async Task<IReadOnlyList<EducationRecordDto>> GetForEmployeeAsync(
        Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await dbContext.EducationRecords
            .Include(e => e.CertificateDocument)
            .AsNoTracking()
            .Where(e => e.EmployeeId == employeeId)
            .OrderByDescending(e => e.QualificationLevel)
            .Select(e => ToDto(e))
            .ToListAsync(cancellationToken);
    }

    public async Task<EducationRecordDto?> CreateAsync(
        Guid employeeId, EducationRecordWriteRequest request, CancellationToken cancellationToken = default)
    {
        var employeeExists = await dbContext.Employees.AnyAsync(e => e.Id == employeeId, cancellationToken);
        if (!employeeExists)
        {
            return null;
        }

        var record = new EducationRecord
        {
            EmployeeId = employeeId,
            QualificationLevel = request.QualificationLevel,
            DegreeName = request.DegreeName,
            InstituteName = request.InstituteName,
            YearOfPassing = request.YearOfPassing,
            Specialization = request.Specialization
        };

        dbContext.EducationRecords.Add(record);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(record);
    }

    public async Task<EducationRecordDto?> UpdateAsync(
        Guid employeeId, Guid id, EducationRecordWriteRequest request, CancellationToken cancellationToken = default)
    {
        var record = await FindAsync(employeeId, id, cancellationToken);
        if (record is null)
        {
            return null;
        }

        record.QualificationLevel = request.QualificationLevel;
        record.DegreeName = request.DegreeName;
        record.InstituteName = request.InstituteName;
        record.YearOfPassing = request.YearOfPassing;
        record.Specialization = request.Specialization;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(record);
    }

    public async Task<bool> DeleteAsync(Guid employeeId, Guid id, CancellationToken cancellationToken = default)
    {
        var record = await FindAsync(employeeId, id, cancellationToken);
        if (record is null)
        {
            return false;
        }

        dbContext.EducationRecords.Remove(record);
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<EducationRecordDto?> UpdateVerificationStatusAsync(
        Guid employeeId, Guid id, VerificationStatus status, CancellationToken cancellationToken = default)
    {
        var record = await FindAsync(employeeId, id, cancellationToken);
        if (record is null)
        {
            return null;
        }

        record.VerificationStatus = status;
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(record);
    }

    public async Task<AttachCertificateResult?> AttachCertificateAsync(
        Guid employeeId, Guid id, Stream content, string fileName, string contentType, long sizeBytes,
        CancellationToken cancellationToken = default)
    {
        var record = await FindAsync(employeeId, id, cancellationToken);
        if (record is null)
        {
            return null;
        }

        if (sizeBytes > MaxFileSizeBytes)
        {
            return AttachCertificateResult.Failure("File exceeds the 10MB limit.");
        }

        if (!AllowedContentTypes.Contains(contentType))
        {
            return AttachCertificateResult.Failure("Only PDF, JPG, and PNG files are accepted.");
        }

        var previousDocumentId = record.CertificateDocumentId;

        var storageKey = await fileStorageService.SaveAsync(content, fileName, cancellationToken);
        var document = new Document
        {
            TenantId = record.TenantId,
            FileName = fileName,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            StorageKey = storageKey,
            UploadedAt = DateTimeOffset.UtcNow
        };

        dbContext.Documents.Add(document);
        record.CertificateDocument = document;
        await dbContext.SaveChangesAsync(cancellationToken);

        if (previousDocumentId is not null)
        {
            var previousDocument = await dbContext.Documents.FindAsync([previousDocumentId], cancellationToken);
            if (previousDocument is not null)
            {
                await fileStorageService.DeleteAsync(previousDocument.StorageKey, cancellationToken);
                dbContext.Documents.Remove(previousDocument);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        return AttachCertificateResult.Success(ToDto(record));
    }

    private Task<EducationRecord?> FindAsync(Guid employeeId, Guid id, CancellationToken cancellationToken) =>
        dbContext.EducationRecords
            .Include(e => e.CertificateDocument)
            .FirstOrDefaultAsync(e => e.Id == id && e.EmployeeId == employeeId, cancellationToken);

    private static EducationRecordDto ToDto(EducationRecord record) => new(
        record.Id,
        record.EmployeeId,
        record.QualificationLevel,
        record.DegreeName,
        record.InstituteName,
        record.YearOfPassing,
        record.Specialization,
        record.VerificationStatus,
        record.CertificateDocumentId,
        record.CertificateDocument?.FileName);
}
