namespace TSTHRMS.Application.Common;

/// <summary>
/// The 4 access levels from the Core HR spec (Section 14). Fixed set for now - not
/// user-configurable, since payroll/compliance logic keys off these exact roles.
/// </summary>
public static class RoleNames
{
    public const string HRAdmin = "HRAdmin";
    public const string HRBP = "HRBP";
    public const string Manager = "Manager";
    public const string Employee = "Employee";

    public static readonly string[] All = [HRAdmin, HRBP, Manager, Employee];
}
