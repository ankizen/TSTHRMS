using TSTHRMS.Application.Common.Dtos;
using TSTHRMS.Application.Documents.Dtos;

namespace TSTHRMS.Application.Documents;

public interface IDocumentRepositoryService
{
    /// <summary>The consolidated Section 10 view - every document attached to this employee
    /// anywhere in Core HR, newest first.</summary>
    Task<IReadOnlyList<DocumentSummaryDto>> GetForEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);

    Task<AttachDocumentResult<DocumentSummaryDto>?> UploadAsync(
        Guid employeeId, EmployeeDocumentWriteRequest request,
        Stream content, string fileName, string contentType, long sizeBytes,
        CancellationToken cancellationToken = default);

    /// <summary>Only removes standalone uploads (EmployeeDocument rows) - documents surfaced
    /// from other records are deleted through their own slice's endpoint.</summary>
    Task<bool> DeleteAsync(Guid employeeId, Guid employeeDocumentId, CancellationToken cancellationToken = default);
}
