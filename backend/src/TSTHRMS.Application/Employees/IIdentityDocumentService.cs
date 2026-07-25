using TSTHRMS.Application.Common.Dtos;
using TSTHRMS.Application.Employees.Dtos;

namespace TSTHRMS.Application.Employees;

public interface IIdentityDocumentService
{
    Task<IReadOnlyList<IdentityDocumentDto>> GetForEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);

    /// <summary>Null return means the employee wasn't found; a Failure result means a document
    /// of this type already exists for the employee (use Update instead).</summary>
    Task<IdentityDocumentUpsertResult?> CreateAsync(Guid employeeId, IdentityDocumentWriteRequest request, CancellationToken cancellationToken = default);

    Task<IdentityDocumentDto?> UpdateAsync(Guid employeeId, Guid id, IdentityDocumentWriteRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid employeeId, Guid id, CancellationToken cancellationToken = default);

    Task<AttachDocumentResult<IdentityDocumentDto>?> AttachProofAsync(
        Guid employeeId, Guid id, Stream content, string fileName, string contentType, long sizeBytes,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the unmasked number and writes a logged Revealed audit entry.</summary>
    Task<IdentityNumberRevealDto?> RevealNumberAsync(Guid employeeId, Guid id, CancellationToken cancellationToken = default);
}
