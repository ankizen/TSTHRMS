using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TSTHRMS.Application.Common.Interfaces;

namespace TSTHRMS.Infrastructure.Identity;

public class UserDirectory(UserManager<ApplicationUser> userManager) : IUserDirectory
{
    public async Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(
        IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        return await userManager.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Email, u.UserName })
            .ToDictionaryAsync(u => u.Id, u => u.Email ?? u.UserName ?? u.Id.ToString(), cancellationToken);
    }
}
