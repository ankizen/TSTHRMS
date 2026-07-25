namespace TSTHRMS.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }

    /// <summary>Default-implemented (rather than a required member) so the many existing test
    /// fakes across the integration test suite don't all need updating just because a new,
    /// mostly-null-in-tests claim was added.</summary>
    Guid? EmployeeId => null;

    /// <summary>HRBP-only scope narrowing - see ApplicationUser.AssignedLegalEntityId/AssignedProductId.</summary>
    Guid? AssignedLegalEntityId => null;
    Guid? AssignedProductId => null;

    IReadOnlyCollection<string> Roles => [];
}
