namespace TSTHRMS.Application.Common;

/// <summary>
/// The Core HR spec's Section 10 upload rule (PDF/JPG/PNG, 10MB cap) applies everywhere a
/// document gets attached to a record - shared here so every consumer enforces it identically.
/// </summary>
public static class DocumentValidation
{
    public const long MaxFileSizeBytes = 10 * 1024 * 1024;

    private static readonly HashSet<string> AllowedContentTypes =
        new(StringComparer.OrdinalIgnoreCase) { "application/pdf", "image/jpeg", "image/png" };

    /// <summary>Null means valid; otherwise the user-facing rejection reason.</summary>
    public static string? Validate(long sizeBytes, string contentType)
    {
        if (sizeBytes > MaxFileSizeBytes)
        {
            return "File exceeds the 10MB limit.";
        }

        if (!AllowedContentTypes.Contains(contentType))
        {
            return "Only PDF, JPG, and PNG files are accepted.";
        }

        return null;
    }
}
