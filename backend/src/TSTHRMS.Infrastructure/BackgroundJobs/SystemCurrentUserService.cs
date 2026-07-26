using TSTHRMS.Application.Common.Interfaces;

namespace TSTHRMS.Infrastructure.BackgroundJobs;

/// <summary>ICurrentUserService for actions a background job takes on its own, not on behalf of
/// any signed-in user - AuditableEntity.ModifiedBy simply stays null for these changes.</summary>
public class SystemCurrentUserService : ICurrentUserService
{
    public Guid? UserId => null;
}
