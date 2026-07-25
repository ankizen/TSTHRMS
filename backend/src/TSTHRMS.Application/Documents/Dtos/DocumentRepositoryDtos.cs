using TSTHRMS.Domain.Documents;

namespace TSTHRMS.Application.Documents.Dtos;

/// <summary>One row per document, merged from every source that can attach a file to an
/// employee (standalone uploads, education certificates, previous-employment letters,
/// identity proofs, nominee consent forms).</summary>
public record DocumentSummaryDto(
    Guid DocumentId,
    string FileName,
    string Category,
    string? Context,
    DateTimeOffset UploadedAt,
    /// <summary>Non-null only for standalone uploads (EmployeeDocument rows) - the id needed
    /// to delete this specific attachment. Documents surfaced from other records are managed
    /// through their own slice's UI instead.</summary>
    Guid? StandaloneAttachmentId);

public record EmployeeDocumentWriteRequest(EmployeeDocumentCategory Category, string? Notes);
