using TSTHRMS.Application.Employees.Dtos;

namespace TSTHRMS.Application.Employees;

public interface IFamilyService
{
    Task<IReadOnlyList<FamilyMemberDto>> GetForEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);

    Task<FamilyMemberDto?> CreateAsync(Guid employeeId, FamilyMemberWriteRequest request, CancellationToken cancellationToken = default);

    Task<FamilyMemberDto?> UpdateAsync(Guid employeeId, Guid id, FamilyMemberWriteRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid employeeId, Guid id, CancellationToken cancellationToken = default);
}
