import { apiClient } from "@/lib/api-client"
import type { EmployeeCustomFieldValue } from "./types"

export async function getEmployeeCustomFieldValues(employeeId: string): Promise<EmployeeCustomFieldValue[]> {
  const { data } = await apiClient.get<EmployeeCustomFieldValue[]>(`/employees/${employeeId}/custom-fields`)
  return data
}

export async function setEmployeeCustomFieldValues(
  employeeId: string,
  values: { definitionId: string; value: string | null }[],
): Promise<EmployeeCustomFieldValue[]> {
  const { data } = await apiClient.put<EmployeeCustomFieldValue[]>(`/employees/${employeeId}/custom-fields`, { values })
  return data
}
