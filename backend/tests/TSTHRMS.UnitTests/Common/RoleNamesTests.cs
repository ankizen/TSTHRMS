using TSTHRMS.Application.Common;

namespace TSTHRMS.UnitTests.Common;

public class RoleNamesTests
{
    [Fact]
    public void All_contains_exactly_the_four_core_hr_access_levels()
    {
        Assert.Equal(
            [RoleNames.HRAdmin, RoleNames.HRBP, RoleNames.Manager, RoleNames.Employee],
            RoleNames.All);
    }
}
