using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Domain.Auditing;
using TSTHRMS.Domain.Common;

namespace TSTHRMS.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Captures a field-level change record for every create/update/delete of an AuditableEntity.
/// Values are stored unmasked (compliance needs the true history); masking sensitive fields
/// for display is applied when the audit log is read, not when it's written.
/// </summary>
public class AuditSaveChangesInterceptor(ITenantContext tenantContext, ICurrentUserService currentUserService)
    : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            CaptureAuditLogs(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void CaptureAuditLogs(DbContext context)
    {
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.Entity is AuditableEntity and not AuditLog
                        && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        if (entries.Count == 0)
        {
            return;
        }

        var auditLogs = new List<AuditLog>();

        foreach (var entry in entries)
        {
            var changes = new List<AuditFieldChange>();

            foreach (var property in entry.Properties)
            {
                if (property.Metadata.IsPrimaryKey())
                {
                    continue;
                }

                var isSensitive = entry.Entity.GetType()
                    .GetProperty(property.Metadata.Name)?
                    .GetCustomAttribute<SensitiveAttribute>() is not null;

                var change = entry.State switch
                {
                    EntityState.Added when property.CurrentValue is not null =>
                        new AuditFieldChange(property.Metadata.Name, null, property.CurrentValue.ToString(), isSensitive),
                    EntityState.Modified when property.IsModified && !Equals(property.OriginalValue, property.CurrentValue) =>
                        new AuditFieldChange(property.Metadata.Name, property.OriginalValue?.ToString(), property.CurrentValue?.ToString(), isSensitive),
                    EntityState.Deleted =>
                        new AuditFieldChange(property.Metadata.Name, property.OriginalValue?.ToString(), null, isSensitive),
                    _ => null
                };

                if (change is not null)
                {
                    changes.Add(change);
                }
            }

            if (changes.Count == 0)
            {
                continue;
            }

            var action = entry.State switch
            {
                EntityState.Added => AuditAction.Created,
                EntityState.Modified => AuditAction.Updated,
                EntityState.Deleted => AuditAction.Deleted,
                _ => throw new InvalidOperationException($"Unexpected entity state {entry.State}")
            };

            var tenantId = entry.Entity is ITenantScoped scoped ? scoped.TenantId : tenantContext.TenantId;

            auditLogs.Add(new AuditLog
            {
                TenantId = tenantId,
                EntityName = entry.Entity.GetType().Name,
                EntityId = ((AuditableEntity)entry.Entity).Id.ToString(),
                Action = action,
                ChangedByUserId = currentUserService.UserId,
                ChangedAt = DateTimeOffset.UtcNow,
                ChangesJson = JsonSerializer.Serialize(changes)
            });
        }

        if (auditLogs.Count > 0)
        {
            context.Set<AuditLog>().AddRange(auditLogs);
        }
    }
}
