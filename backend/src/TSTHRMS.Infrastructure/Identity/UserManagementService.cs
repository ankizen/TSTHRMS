using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Application.Users;
using TSTHRMS.Application.Users.Dtos;

namespace TSTHRMS.Infrastructure.Identity;

public class UserManagementService(
    UserManager<ApplicationUser> userManager,
    IApplicationDbContext dbContext,
    ITenantContext tenantContext) : IUserManagementService
{
    public async Task<IReadOnlyList<UserSummaryDto>> GetListAsync(CancellationToken cancellationToken = default)
    {
        var users = await userManager.Users
            .Where(u => u.TenantId == tenantContext.TenantId)
            .ToListAsync(cancellationToken);

        var employeeIds = users.Where(u => u.EmployeeId is not null).Select(u => u.EmployeeId!.Value).ToList();
        var legalEntityIds = users.Where(u => u.AssignedLegalEntityId is not null).Select(u => u.AssignedLegalEntityId!.Value).ToList();
        var productIds = users.Where(u => u.AssignedProductId is not null).Select(u => u.AssignedProductId!.Value).ToList();

        var employeeNames = await dbContext.Employees.AsNoTracking()
            .Where(e => employeeIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.FirstName + " " + e.LastName, cancellationToken);
        var legalEntityNames = await dbContext.LegalEntities.AsNoTracking()
            .Where(e => legalEntityIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.Name, cancellationToken);
        var productNames = await dbContext.Products.AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

        var result = new List<UserSummaryDto>();
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            result.Add(new UserSummaryDto(
                user.Id,
                user.Email ?? string.Empty,
                roles.ToList(),
                user.EmployeeId,
                user.EmployeeId is not null && employeeNames.TryGetValue(user.EmployeeId.Value, out var name) ? name : null,
                user.AssignedLegalEntityId,
                user.AssignedLegalEntityId is not null && legalEntityNames.TryGetValue(user.AssignedLegalEntityId.Value, out var entityName) ? entityName : null,
                user.AssignedProductId,
                user.AssignedProductId is not null && productNames.TryGetValue(user.AssignedProductId.Value, out var productName) ? productName : null));
        }

        return result;
    }

    public async Task<UserCreationResult> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        if (!RoleNames.All.Contains(request.Role))
        {
            return UserCreationResult.Failure([$"'{request.Role}' is not a valid role."]);
        }

        var employee = await dbContext.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken);
        if (employee is null)
        {
            return UserCreationResult.Failure(["Employee not found."]);
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            TenantId = tenantContext.TenantId,
            EmployeeId = request.EmployeeId,
            EmailConfirmed = true,
            // Only meaningful for the HRBP role, but harmless to store regardless - the scope
            // check on the read side only ever runs for HRBP-role callers.
            AssignedLegalEntityId = request.AssignedLegalEntityId,
            AssignedProductId = request.AssignedProductId
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return UserCreationResult.Failure(createResult.Errors.Select(e => e.Description));
        }

        await userManager.AddToRoleAsync(user, request.Role);

        return UserCreationResult.Success(new UserSummaryDto(
            user.Id, user.Email, [request.Role], user.EmployeeId, $"{employee.FirstName} {employee.LastName}",
            user.AssignedLegalEntityId, null, user.AssignedProductId, null));
    }

    public async Task<bool> DeleteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantContext.TenantId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        var result = await userManager.DeleteAsync(user);
        return result.Succeeded;
    }
}
