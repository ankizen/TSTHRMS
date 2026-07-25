using Microsoft.Extensions.Options;
using TSTHRMS.Application.Common.Interfaces;

namespace TSTHRMS.Infrastructure.Storage;

/// <summary>
/// Stores files on local disk under a fixed root. Storage keys are always GUID-generated
/// here (never derived from user input), and every read/delete re-validates the resolved
/// path stays under the root - defence in depth against path traversal even though a
/// malformed key should never occur in practice.
/// </summary>
public class LocalFileStorageService(IOptions<LocalFileStorageOptions> options) : IFileStorageService
{
    private readonly string _rootPath = Path.GetFullPath(options.Value.RootPath);

    public async Task<string> SaveAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_rootPath);

        var extension = Path.GetExtension(fileName);
        var storageKey = $"{Guid.NewGuid():N}{extension}";
        var fullPath = ResolvePath(storageKey);

        await using var fileStream = File.Create(fullPath);
        await content.CopyToAsync(fileStream, cancellationToken);

        return storageKey;
    }

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(storageKey);
        Stream? stream = File.Exists(fullPath) ? File.OpenRead(fullPath) : null;
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(storageKey);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    private string ResolvePath(string storageKey)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, storageKey));
        if (!fullPath.StartsWith(_rootPath + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Invalid storage key.");
        }

        return fullPath;
    }
}
