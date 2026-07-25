using System.Text.RegularExpressions;

namespace TSTHRMS.Application.Common;

/// <summary>Turns a title into a URL-safe, SEO-friendly slug (Career Site Section 1's "each job
/// gets its own URL"). Callers are responsible for dedup-checking against existing rows and
/// appending a numeric suffix if needed.</summary>
public static partial class SlugGenerator
{
    public static string FromTitle(string title)
    {
        var lowered = title.Trim().ToLowerInvariant();
        var withDashes = NonAlphaNumeric().Replace(lowered, "-");
        var collapsed = MultipleDashes().Replace(withDashes, "-").Trim('-');
        return string.IsNullOrEmpty(collapsed) ? "role" : collapsed;
    }

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex NonAlphaNumeric();

    [GeneratedRegex(@"-{2,}")]
    private static partial Regex MultipleDashes();
}
