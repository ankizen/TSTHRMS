using Microsoft.EntityFrameworkCore;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Common.Dtos;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Application.Employees.Dtos;
using TSTHRMS.Domain.Employees;

namespace TSTHRMS.Application.Employees;

public class PreviousEmploymentService(
    IApplicationDbContext dbContext,
    IFileStorageService fileStorageService,
    ICurrentUserService currentUserService)
    : IPreviousEmploymentService
{
    public async Task<IReadOnlyList<PreviousEmploymentRecordDto>> GetForEmployeeAsync(
        Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await dbContext.PreviousEmploymentRecords
            .Include(p => p.RelievingLetterDocument)
            .Include(p => p.LastSalarySlipDocument)
            .AsNoTracking()
            .Where(p => p.EmployeeId == employeeId)
            .OrderByDescending(p => p.DateOfLeaving)
            .Select(p => ToDto(p))
            .ToListAsync(cancellationToken);
    }

    public async Task<PreviousEmploymentRecordDto?> CreateAsync(
        Guid employeeId, PreviousEmploymentRecordWriteRequest request, CancellationToken cancellationToken = default)
    {
        var employeeExists = await dbContext.Employees.AnyAsync(e => e.Id == employeeId, cancellationToken);
        if (!employeeExists)
        {
            return null;
        }

        var record = new PreviousEmploymentRecord
        {
            EmployeeId = employeeId,
            CompanyName = request.CompanyName,
            Designation = request.Designation,
            YearsOfExperience = request.YearsOfExperience,
            DateOfJoining = request.DateOfJoining,
            DateOfLeaving = request.DateOfLeaving,
            ReasonForLeaving = request.ReasonForLeaving,
            PreviousUan = request.PreviousUan
        };

        dbContext.PreviousEmploymentRecords.Add(record);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(record);
    }

    public async Task<PreviousEmploymentRecordDto?> UpdateAsync(
        Guid employeeId, Guid id, PreviousEmploymentRecordWriteRequest request, CancellationToken cancellationToken = default)
    {
        var record = await FindAsync(employeeId, id, cancellationToken);
        if (record is null)
        {
            return null;
        }

        record.CompanyName = request.CompanyName;
        record.Designation = request.Designation;
        record.YearsOfExperience = request.YearsOfExperience;
        record.DateOfJoining = request.DateOfJoining;
        record.DateOfLeaving = request.DateOfLeaving;
        record.ReasonForLeaving = request.ReasonForLeaving;
        record.PreviousUan = request.PreviousUan;

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

        record.IsDeleted = true;
        record.DeletedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<AttachDocumentResult<PreviousEmploymentRecordDto>?> AttachDocumentAsync(
        Guid employeeId, Guid id, PreviousEmploymentDocumentSlot slot,
        Stream content, string fileName, string contentType, long sizeBytes,
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
            return AttachDocumentResult<PreviousEmploymentRecordDto>.Failure(validationError);
        }

        var previousDocumentId = slot switch
        {
            PreviousEmploymentDocumentSlot.RelievingLetter => record.RelievingLetterDocumentId,
            PreviousEmploymentDocumentSlot.LastSalarySlip => record.LastSalarySlipDocumentId,
            _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, null)
        };

        var document = await DocumentAttachmentHelper.SaveAndReplaceAsync(
            dbContext, fileStorageService, record.TenantId, previousDocumentId,
            content, fileName, contentType, sizeBytes, currentUserService.UserId, cancellationToken);

        switch (slot)
        {
            case PreviousEmploymentDocumentSlot.RelievingLetter:
                record.RelievingLetterDocument = document;
                break;
            case PreviousEmploymentDocumentSlot.LastSalarySlip:
                record.LastSalarySlipDocument = document;
                break;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return AttachDocumentResult<PreviousEmploymentRecordDto>.Success(ToDto(record));
    }

    private Task<PreviousEmploymentRecord?> FindAsync(Guid employeeId, Guid id, CancellationToken cancellationToken) =>
        dbContext.PreviousEmploymentRecords
            .Include(p => p.RelievingLetterDocument)
            .Include(p => p.LastSalarySlipDocument)
            .FirstOrDefaultAsync(p => p.Id == id && p.EmployeeId == employeeId, cancellationToken);

    private static PreviousEmploymentRecordDto ToDto(PreviousEmploymentRecord record) => new(
        record.Id,
        record.EmployeeId,
        record.CompanyName,
        record.Designation,
        record.YearsOfExperience,
        record.DateOfJoining,
        record.DateOfLeaving,
        record.ReasonForLeaving,
        record.PreviousUan,
        record.RelievingLetterDocumentId,
        record.RelievingLetterDocument?.FileName,
        record.LastSalarySlipDocumentId,
        record.LastSalarySlipDocument?.FileName);
}
