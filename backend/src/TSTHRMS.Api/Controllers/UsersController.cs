using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Users;
using TSTHRMS.Application.Users.Dtos;

namespace TSTHRMS.Api.Controllers;

/// <summary>Section 14: provisions logins for existing Employee records. HRAdmin-only - even an
/// HRBP can't create or remove accounts, only HR Admin manages who gets access.</summary>
[ApiController]
[Route("api/users")]
[Authorize(Roles = RoleNames.HRAdmin)]
public class UsersController(IUserManagementService userManagementService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserSummaryDto>>> GetList(CancellationToken cancellationToken)
    {
        var users = await userManagementService.GetListAsync(cancellationToken);
        return Ok(users);
    }

    [HttpPost]
    public async Task<ActionResult<UserSummaryDto>> Create(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await userManagementService.CreateAsync(request, cancellationToken);
        return result.Succeeded ? Ok(result.User) : BadRequest(new { errors = result.Errors });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await userManagementService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
