using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TSTHRMS.Application.Auditing.Dtos;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Domain.Auditing;
using TSTHRMS.Domain.Documents;
using TSTHRMS.Domain.Employees;

namespace TSTHRMS.Application.Auditing;

/// <summary>
/// AuditLog rows are keyed by (EntityName, EntityId) of whatever row actually changed - a child
/// record like an EducationRecord logs under its own id, not the employee's. To show one unified
/// "everything that happened to this employee" timeline, this service first collects the ids of
/// every child record that belongs to the employee, then pulls every AuditLog row whose
/// (EntityName, EntityId) matches the employee itself or one of those child ids.
/// </summary>
public class AuditLogService(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IUserDirectory userDirectory) : IAuditLogService
{
    public async Task<IReadOnlyList<AuditLogEntryDto>?> GetEmployeeHistoryAsync(
        Guid employeeId, CancellationToken cancellationToken = default)
    {
        var employeeExists = await dbContext.Employees.AnyAsync(e => e.Id == employeeId, cancellationToken);
        if (!employeeExists)
        {
            return null;
        }

        var entityIdsByName = await CollectRelatedEntityIdsAsync(employeeId, cancellationToken);
        var logs = await LoadLogsAsync(entityIdsByName, cancellationToken);

        return await ToDtosAsync(logs, reveal: false, cancellationToken);
    }

    public async Task<AuditLogEntryDto?> RevealEntryAsync(
        Guid employeeId, Guid auditLogId, CancellationToken cancellationToken = default)
    {
        var entityIdsByName = await CollectRelatedEntityIdsAsync(employeeId, cancellationToken);

        var log = await dbContext.AuditLogs.FirstOrDefaultAsync(a => a.Id == auditLogId, cancellationToken);
        if (log is null
            || !entityIdsByName.TryGetValue(log.EntityName, out var idsForEntity)
            || !idsForEntity.Contains(log.EntityId))
        {
            return null;
        }

        var changes = JsonSerializer.Deserialize<List<AuditFieldChange>>(log.ChangesJson) ?? [];
        var sensitiveFields = changes.Where(c => c.IsSensitive).Select(c => c.PropertyName).ToList();

        if (sensitiveFields.Count > 0)
        {
            dbContext.AuditLogs.Add(new AuditLog
            {
                TenantId = log.TenantId,
                EntityName = log.EntityName,
                EntityId = log.EntityId,
                Action = AuditAction.Revealed,
                ChangedByUserId = currentUserService.UserId,
                ChangedAt = DateTimeOffset.UtcNow,
                ChangesJson = JsonSerializer.Serialize(
                    sensitiveFields.Select(field => new AuditFieldChange(field, null, null, true)))
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var dtos = await ToDtosAsync([log], reveal: true, cancellationToken);
        return dtos[0];
    }

    private async Task<Dictionary<string, HashSet<string>>> CollectRelatedEntityIdsAsync(
        Guid employeeId, CancellationToken cancellationToken)
    {
        // Explicit per-table queries rather than reflection - clearer and every table here has
        // a plain `EmployeeId` FK plus its own `Id`, so there is no generic shortcut worth taking.
        var educationIds = await dbContext.EducationRecords
            .Where(e => e.EmployeeId == employeeId).Select(e => e.Id).ToListAsync(cancellationToken);
        var familyIds = await dbContext.FamilyMembers
            .Where(e => e.EmployeeId == employeeId).Select(e => e.Id).ToListAsync(cancellationToken);
        var previousEmploymentIds = await dbContext.PreviousEmploymentRecords
            .Where(e => e.EmployeeId == employeeId).Select(e => e.Id).ToListAsync(cancellationToken);
        var identityDocumentIds = await dbContext.IdentityDocuments
            .Where(e => e.EmployeeId == employeeId).Select(e => e.Id).ToListAsync(cancellationToken);
        var nomineeIds = await dbContext.Nominees
            .Where(e => e.EmployeeId == employeeId).Select(e => e.Id).ToListAsync(cancellationToken);
        var employeeDocumentIds = await dbContext.EmployeeDocuments
            .Where(e => e.EmployeeId == employeeId).Select(e => e.Id).ToListAsync(cancellationToken);

        return new Dictionary<string, HashSet<string>>
        {
            [nameof(Employee)] = [employeeId.ToString()],
            [nameof(EducationRecord)] = educationIds.Select(id => id.ToString()).ToHashSet(),
            [nameof(FamilyMember)] = familyIds.Select(id => id.ToString()).ToHashSet(),
            [nameof(PreviousEmploymentRecord)] = previousEmploymentIds.Select(id => id.ToString()).ToHashSet(),
            [nameof(IdentityDocument)] = identityDocumentIds.Select(id => id.ToString()).ToHashSet(),
            [nameof(Nominee)] = nomineeIds.Select(id => id.ToString()).ToHashSet(),
            [nameof(EmployeeDocument)] = employeeDocumentIds.Select(id => id.ToString()).ToHashSet(),
        };
    }

    private async Task<List<AuditLog>> LoadLogsAsync(
        Dictionary<string, HashSet<string>> entityIdsByName, CancellationToken cancellationToken)
    {
        var entityNames = entityIdsByName.Keys.ToList();

        var candidates = await dbContext.AuditLogs
            .AsNoTracking()
            .Where(a => entityNames.Contains(a.EntityName))
            .OrderByDescending(a => a.ChangedAt)
            .ToListAsync(cancellationToken);

        return candidates.Where(a => entityIdsByName[a.EntityName].Contains(a.EntityId)).ToList();
    }

    private async Task<List<AuditLogEntryDto>> ToDtosAsync(
        List<AuditLog> logs, bool reveal, CancellationToken cancellationToken)
    {
        var userIds = logs
            .Where(l => l.ChangedByUserId is not null)
            .Select(l => l.ChangedByUserId!.Value)
            .Distinct()
            .ToList();
        var displayNames = await userDirectory.GetDisplayNamesAsync(userIds, cancellationToken);

        return logs.Select(log =>
        {
            var changes = JsonSerializer.Deserialize<List<AuditFieldChange>>(log.ChangesJson) ?? [];

            var changeDtos = changes.Select(c => new AuditFieldChangeDto(
                c.PropertyName,
                c.IsSensitive && !reveal ? Masking.MaskLastFour(c.OldValue) : c.OldValue,
                c.IsSensitive && !reveal ? Masking.MaskLastFour(c.NewValue) : c.NewValue,
                c.IsSensitive)).ToList();

            var displayName = log.ChangedByUserId is not null && displayNames.TryGetValue(log.ChangedByUserId.Value, out var name)
                ? name
                : null;

            return new AuditLogEntryDto(
                log.Id, log.EntityName, log.EntityId, log.Action, log.ChangedByUserId, displayName, log.ChangedAt, changeDtos);
        }).ToList();
    }
}
