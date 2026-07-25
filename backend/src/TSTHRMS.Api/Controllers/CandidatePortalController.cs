using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TSTHRMS.Application.Recruitment;
using TSTHRMS.Application.Recruitment.Dtos;

namespace TSTHRMS.Api.Controllers;

/// <summary>
/// Section 3's Candidate Portal - deliberately not role-restricted, since a candidate's JWT
/// carries no roles at all (see JwtTokenGenerator.GenerateCandidateAccessToken). Every method in
/// ICandidatePortalService is self-scoped via ICandidateContext, so there's no id parameter here
/// a caller could tamper with to see someone else's applications.
/// </summary>
[ApiController]
[Authorize]
[Route("api/candidate-portal")]
public class CandidatePortalController(ICandidatePortalService candidatePortalService) : ControllerBase
{
    [HttpGet("applications")]
    public async Task<ActionResult<IReadOnlyList<MyApplicationDto>>> GetMyApplications(CancellationToken cancellationToken)
    {
        var applications = await candidatePortalService.GetMyApplicationsAsync(cancellationToken);
        return Ok(applications);
    }
}
