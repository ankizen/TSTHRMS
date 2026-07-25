using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Domain.Documents;

namespace TSTHRMS.Application.Common;

/// <summary>
/// Shared "store the file, create its Document row, and mark whatever it's replacing for
/// removal" flow used by every document-upload slot (education certificates, previous
/// employment letters/payslips, and more once the full Document Repository lands).
/// </summary>
public static class DocumentAttachmentHelper
{
    /// <summary>Caller assigns the returned Document to the owning record's navigation
    /// property and then calls SaveChangesAsync once - that single save both attaches the
    /// new document and removes the old one in the same transaction.</summary>
    public static async Task<Document> SaveAndReplaceAsync(
        IApplicationDbContext dbContext,
        IFileStorageService fileStorageService,
        Guid tenantId,
        Guid? previousDocumentId,
        Stream content,
        string fileName,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken)
    {
        var storageKey = await fileStorageService.SaveAsync(content, fileName, cancellationToken);

        var document = new Document
        {
            TenantId = tenantId,
            FileName = fileName,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            StorageKey = storageKey,
            UploadedAt = DateTimeOffset.UtcNow
        };

        dbContext.Documents.Add(document);

        if (previousDocumentId is not null)
        {
            var previousDocument = await dbContext.Documents.FindAsync([previousDocumentId], cancellationToken);
            if (previousDocument is not null)
            {
                await fileStorageService.DeleteAsync(previousDocument.StorageKey, cancellationToken);
                dbContext.Documents.Remove(previousDocument);
            }
        }

        return document;
    }
}
