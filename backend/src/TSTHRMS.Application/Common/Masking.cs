namespace TSTHRMS.Application.Common;

public static class Masking
{
    /// <summary>Masks everything but the last 4 characters, e.g. "••••••1234".</summary>
    public static string? MaskLastFour(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return value.Length <= 4
            ? new string('•', value.Length)
            : $"{new string('•', value.Length - 4)}{value[^4..]}";
    }
}
