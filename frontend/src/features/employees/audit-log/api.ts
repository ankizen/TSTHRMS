import { apiClient } from "@/lib/api-client"
import type { AuditLogEntry } from "./types"

export async function getEmployeeAuditLog(employeeId: string): Promise<AuditLogEntry[]> {
  const { data } = await apiClient.get<AuditLogEntry[]>(`/employees/${employeeId}/audit-log`)
  return data
}

export async function revealAuditLogEntry(employeeId: string, auditLogId: string): Promise<AuditLogEntry> {
  const { data } = await apiClient.post<AuditLogEntry>(`/employees/${employeeId}/audit-log/${auditLogId}/reveal`)
  return data
}
