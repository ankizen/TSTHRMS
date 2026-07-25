using TSTHRMS.Domain.Employees;

namespace TSTHRMS.Application.Employees.Dtos;

// NumberDisplay is masked for Aadhaar, plain for everything else - matches the spec exactly.
public record IdentityDocumentDto(
    Guid Id,
    Guid EmployeeId,
    IdentityDocumentType DocumentType,
    string NumberDisplay,
    DateOnly? ExpiryDate,
    Guid? ProofDocumentId,
    string? ProofFileName);

/// <summary>Shared by create and update. DocumentType is immutable after creation - on update
/// the service ignores it for the mutation itself and only uses it to pick the right
/// validation rules (so the DTO stays self-contained for the validator).</summary>
public record IdentityDocumentWriteRequest(IdentityDocumentType DocumentType, string Number, DateOnly? ExpiryDate);

public record IdentityDocumentUpsertResult(bool Succeeded, IdentityDocumentDto? Record, string? Error)
{
    public static IdentityDocumentUpsertResult Success(IdentityDocumentDto record) => new(true, record, null);
    public static IdentityDocumentUpsertResult Failure(string error) => new(false, null, error);
}

public record IdentityNumberRevealDto(string Number);
