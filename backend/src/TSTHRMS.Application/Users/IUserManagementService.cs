using TSTHRMS.Application.Users.Dtos;

namespace TSTHRMS.Application.Users;

/// <summary>HRAdmin-only: provisions logins for existing Employee records and assigns their
/// role (and, for HRBP, their legal entity/product scope). There is no self-registration -
/// every account starts from an Employee row that already exists.</summary>
public interface IUserManagementService
{
    Task<IReadOnlyList<UserSummaryDto>> GetListAsync(CancellationToken cancellationToken = default);

    Task<UserCreationResult> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid userId, CancellationToken cancellationToken = default);
}
