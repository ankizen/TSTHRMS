using TSTHRMS.Domain.Common;

namespace TSTHRMS.Domain.Documents;

/// <summary>
/// Minimal, deliberately generic file record - metadata lives here, bytes live on disk via
/// IFileStorageService. This is the seed of the full Document Repository (Core HR Section 10,
/// a later slice): more categories, access control, and a browsing UI get added there without
/// needing to touch this shape.
/// </summary>
public class Document : TenantScopedEntity
{
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public long SizeBytes { get; set; }

    /// <summary>Opaque key into IFileStorageService - never a user-controlled path.</summary>
    public required string StorageKey { get; set; }

    public Guid? UploadedByUserId { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
}
