import type { EmployeeEditRequest } from "@/features/my/types"
import { apiClient } from "@/lib/api-client"

export async function getPendingEditRequests(): Promise<EmployeeEditRequest[]> {
  const { data } = await apiClient.get<EmployeeEditRequest[]>("/employee-edit-requests/pending")
  return data
}

export async function approveEditRequest(id: string, reviewNote: string | null): Promise<EmployeeEditRequest> {
  const { data } = await apiClient.post<EmployeeEditRequest>(`/employee-edit-requests/${id}/approve`, { reviewNote })
  return data
}

export async function rejectEditRequest(id: string, reviewNote: string | null): Promise<EmployeeEditRequest> {
  const { data } = await apiClient.post<EmployeeEditRequest>(`/employee-edit-requests/${id}/reject`, { reviewNote })
  return data
}
