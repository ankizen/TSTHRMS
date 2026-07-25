using TSTHRMS.Application.Common.Interfaces;

namespace TSTHRMS.Api.Services;

public class CandidateContext(IHttpContextAccessor httpContextAccessor) : ICandidateContext
{
    public Guid? CandidateId
    {
        get
        {
            var claim = httpContextAccessor.HttpContext?.User.FindFirst("candidate_id")?.Value;
            return claim is not null && Guid.TryParse(claim, out var id) ? id : null;
        }
    }
}
