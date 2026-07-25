using System.Security.Claims;
using TSTHRMS.Application.Common.Interfaces;

namespace TSTHRMS.Api.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid? UserId => ParseGuidClaim(ClaimTypes.NameIdentifier);
    public Guid? EmployeeId => ParseGuidClaim("employee_id");
    public Guid? AssignedLegalEntityId => ParseGuidClaim("assigned_legal_entity_id");
    public Guid? AssignedProductId => ParseGuidClaim("assigned_product_id");

    public IReadOnlyCollection<string> Roles =>
        httpContextAccessor.HttpContext?.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray() ?? [];

    private Guid? ParseGuidClaim(string claimType)
    {
        var claim = httpContextAccessor.HttpContext?.User.FindFirst(claimType)?.Value;
        return claim is not null && Guid.TryParse(claim, out var id) ? id : null;
    }
}
