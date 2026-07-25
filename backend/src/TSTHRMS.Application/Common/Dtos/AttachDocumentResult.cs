namespace TSTHRMS.Application.Common.Dtos;

/// <summary>Null (the outer nullable on the method's return type) means the parent record
/// wasn't found; a Failure here means it was found but the file failed validation.</summary>
public record AttachDocumentResult<T>(bool Succeeded, T? Record, string? Error)
{
    public static AttachDocumentResult<T> Success(T record) => new(true, record, null);
    public static AttachDocumentResult<T> Failure(string error) => new(false, default, error);
}
