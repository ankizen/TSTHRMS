using Microsoft.EntityFrameworkCore;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Common.Dtos;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Application.Employees.Dtos;
using TSTHRMS.Domain.Employees;

namespace TSTHRMS.Application.Employees;

public class EducationService(IApplicationDbContext dbContext, IFileStorageService fileStorageService) : IEducationService
{
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

    public async Task<AttachDocumentResult<EducationRecordDto>?> AttachCertificateAsync(
        Guid employeeId, Guid id, Stream content, string fileName, string contentType, long sizeBytes,
        CancellationToken cancellationToken = default)
    {
        var record = await FindAsync(employeeId, id, cancellationToken);
        if (record is null)
        {
            return null;
        }

        var validationError = DocumentValidation.Validate(sizeBytes, contentType);
        if (validationError is not null)
        {
            return AttachDocumentResult<EducationRecordDto>.Failure(validationError);
        }

        record.CertificateDocument = await DocumentAttachmentHelper.SaveAndReplaceAsync(
            dbContext, fileStorageService, record.TenantId, record.CertificateDocumentId,
            content, fileName, contentType, sizeBytes, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return AttachDocumentResult<EducationRecordDto>.Success(ToDto(record));
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
