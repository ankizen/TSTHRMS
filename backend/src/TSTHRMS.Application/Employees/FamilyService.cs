using Microsoft.EntityFrameworkCore;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Application.Employees.Dtos;
using TSTHRMS.Domain.Employees;

namespace TSTHRMS.Application.Employees;

public class FamilyService(IApplicationDbContext dbContext) : IFamilyService
{
    public async Task<IReadOnlyList<FamilyMemberDto>> GetForEmployeeAsync(
        Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await dbContext.FamilyMembers
            .AsNoTracking()
            .Where(f => f.EmployeeId == employeeId)
            .OrderBy(f => f.Relation).ThenBy(f => f.Name)
            .Select(f => ToDto(f))
            .ToListAsync(cancellationToken);
    }

    public async Task<FamilyMemberDto?> CreateAsync(
        Guid employeeId, FamilyMemberWriteRequest request, CancellationToken cancellationToken = default)
    {
        var employeeExists = await dbContext.Employees.AnyAsync(e => e.Id == employeeId, cancellationToken);
        if (!employeeExists)
        {
            return null;
        }

        var member = new FamilyMember
        {
            EmployeeId = employeeId,
            Relation = request.Relation,
            Name = request.Name,
            DateOfBirth = request.DateOfBirth,
            IsDependent = request.IsDependent,
            IsDifferentlyAbled = request.IsDifferentlyAbled
        };

        dbContext.FamilyMembers.Add(member);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(member);
    }

    public async Task<FamilyMemberDto?> UpdateAsync(
        Guid employeeId, Guid id, FamilyMemberWriteRequest request, CancellationToken cancellationToken = default)
    {
        var member = await FindAsync(employeeId, id, cancellationToken);
        if (member is null)
        {
            return null;
        }

        member.Relation = request.Relation;
        member.Name = request.Name;
        member.DateOfBirth = request.DateOfBirth;
        member.IsDependent = request.IsDependent;
        member.IsDifferentlyAbled = request.IsDifferentlyAbled;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(member);
    }

    public async Task<bool> DeleteAsync(Guid employeeId, Guid id, CancellationToken cancellationToken = default)
    {
        var member = await FindAsync(employeeId, id, cancellationToken);
        if (member is null)
        {
            return false;
        }

        dbContext.FamilyMembers.Remove(member);
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private Task<FamilyMember?> FindAsync(Guid employeeId, Guid id, CancellationToken cancellationToken) =>
        dbContext.FamilyMembers.FirstOrDefaultAsync(f => f.Id == id && f.EmployeeId == employeeId, cancellationToken);

    private static FamilyMemberDto ToDto(FamilyMember member) => new(
        member.Id,
        member.EmployeeId,
        member.Relation,
        member.Name,
        member.DateOfBirth,
        member.IsDependent,
        member.IsDifferentlyAbled);
}
