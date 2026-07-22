namespace TSTHRMS.Domain.Common;

/// <summary>
/// Marks a property that must be masked by default in API responses and audit log views,
/// and whose unmasked value may only be retrieved through an explicit, separately-logged reveal action.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SensitiveAttribute : Attribute;
