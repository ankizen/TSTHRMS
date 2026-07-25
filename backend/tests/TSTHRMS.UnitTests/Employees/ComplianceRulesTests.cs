using TSTHRMS.Application.Employees;

namespace TSTHRMS.UnitTests.Employees;

public class ComplianceRulesTests
{
    [Fact]
    public void IsPfApplicable_true_when_registered_and_at_or_under_threshold()
    {
        Assert.True(ComplianceRules.IsPfApplicable(true, 15000m));
    }

    [Fact]
    public void IsPfApplicable_false_when_over_threshold()
    {
        Assert.False(ComplianceRules.IsPfApplicable(true, 15000.01m));
    }

    [Fact]
    public void IsPfApplicable_false_when_salary_unknown()
    {
        Assert.False(ComplianceRules.IsPfApplicable(true, null));
    }

    [Fact]
    public void IsPfApplicable_false_when_entity_not_registered()
    {
        Assert.False(ComplianceRules.IsPfApplicable(false, 10000m));
    }

    [Fact]
    public void IsEsicApplicable_true_when_registered_and_at_or_under_threshold()
    {
        Assert.True(ComplianceRules.IsEsicApplicable(true, 21000m));
    }

    [Fact]
    public void IsEsicApplicable_false_when_over_threshold()
    {
        Assert.False(ComplianceRules.IsEsicApplicable(true, 21000.01m));
    }

    [Fact]
    public void IsEsicApplicable_false_when_entity_not_registered()
    {
        Assert.False(ComplianceRules.IsEsicApplicable(false, 10000m));
    }

    [Fact]
    public void IsMaharashtraLwfEligible_true_for_maharashtra_with_known_salary()
    {
        Assert.True(ComplianceRules.IsMaharashtraLwfEligible("Maharashtra", 10000m));
    }

    [Fact]
    public void IsMaharashtraLwfEligible_is_case_insensitive()
    {
        Assert.True(ComplianceRules.IsMaharashtraLwfEligible("maharashtra", 10000m));
    }

    [Fact]
    public void IsMaharashtraLwfEligible_false_for_other_states()
    {
        Assert.False(ComplianceRules.IsMaharashtraLwfEligible("Karnataka", 10000m));
    }

    [Fact]
    public void IsMaharashtraLwfEligible_false_when_salary_unknown()
    {
        Assert.False(ComplianceRules.IsMaharashtraLwfEligible("Maharashtra", null));
    }

    [Fact]
    public void IsMaharashtraLwfEligible_false_when_state_unknown()
    {
        Assert.False(ComplianceRules.IsMaharashtraLwfEligible(null, 10000m));
    }
}
