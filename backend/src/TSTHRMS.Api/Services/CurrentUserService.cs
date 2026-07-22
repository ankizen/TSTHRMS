using System.Security.Claims;
using TSTHRMS.Application.Common.Interfaces;

namespace TSTHRMS.Api.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid? UserId
    {
        get
        {
            var claim = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return claim is not null && Guid.TryParse(claim, out var id) ? id : null;
        }
    }
}
