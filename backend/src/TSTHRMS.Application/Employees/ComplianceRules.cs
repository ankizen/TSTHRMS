namespace TSTHRMS.Application.Employees;

/// <summary>
/// PF (Rs 15,000/month) and ESIC (Rs 21,000/month) thresholds are long-standing central
/// statutory limits under the EPF Act and ESI Act. The Maharashtra LWF check below is
/// structural eligibility only (state + salary known), not a contribution amount - LWF slabs
/// vary by state and change periodically by government notification, so verify the exact
/// figure against the current Maharashtra Labour Welfare Fund Act notification before relying
/// on this for actual statutory filings.
/// </summary>
public static class ComplianceRules
{
    public const decimal PfSalaryThreshold = 15000m;
    public const decimal EsicSalaryThreshold = 21000m;

    public static bool IsPfApplicable(bool legalEntityIsPfRegistered, decimal? monthlyGrossSalary) =>
        legalEntityIsPfRegistered && monthlyGrossSalary is not null && monthlyGrossSalary <= PfSalaryThreshold;

    public static bool IsEsicApplicable(bool legalEntityIsEsicRegistered, decimal? monthlyGrossSalary) =>
        legalEntityIsEsicRegistered && monthlyGrossSalary is not null && monthlyGrossSalary <= EsicSalaryThreshold;

    public static bool IsMaharashtraLwfEligible(string? professionalTaxState, decimal? monthlyGrossSalary) =>
        string.Equals(professionalTaxState, "Maharashtra", StringComparison.OrdinalIgnoreCase)
        && monthlyGrossSalary is not null;
}
