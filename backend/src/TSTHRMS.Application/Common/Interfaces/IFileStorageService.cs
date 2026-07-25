namespace TSTHRMS.Application.Common.Interfaces;

/// <summary>
/// Stores file bytes behind an opaque key - callers never construct or interpret the key as
/// a path, which is what keeps the implementation free to change (disk today, blob storage
/// later) without touching anything above this interface.
/// </summary>
public interface IFileStorageService
{
    Task<string> SaveAsync(Stream content, string fileName, CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);

    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}
