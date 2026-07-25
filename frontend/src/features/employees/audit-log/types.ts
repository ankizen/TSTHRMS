export type AuditAction = "Created" | "Updated" | "Deleted" | "Revealed"

export interface AuditFieldChange {
  propertyName: string
  oldValue: string | null
  newValue: string | null
  isSensitive: boolean
}

export interface AuditLogEntry {
  id: string
  entityName: string
  entityId: string
  action: AuditAction
  changedByUserId: string | null
  changedByDisplayName: string | null
  changedAt: string
  changes: AuditFieldChange[]
}
