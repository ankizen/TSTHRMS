namespace TSTHRMS.Application.Employees;

/// <summary>
/// Company-wide default probation length used to auto-calculate ProbationEndDate on create
/// when HR doesn't supply one explicitly. A single constant today; if different entities/roles
/// ever need different defaults, this is the one place to make it configurable.
/// </summary>
public static class ProbationDefaults
{
    public const int DurationMonths = 6;

    /// <summary>How many days out a contract end date counts as "expiring soon" for the UI badge.</summary>
    public const int ContractExpiryWarningDays = 30;
}
