import type { Employee } from "@/features/employees/types"
import { apiClient } from "@/lib/api-client"
import type { DirectReportSummary, EmployeeEditRequest, SubmitEditRequestItem } from "./types"

export async function getMyProfile(): Promise<Employee> {
  const { data } = await apiClient.get<Employee>("/my/profile")
  return data
}

export async function getMyDirectReports(): Promise<DirectReportSummary[]> {
  const { data } = await apiClient.get<DirectReportSummary[]>("/my/direct-reports")
  return data
}

export async function getMyEditRequests(): Promise<EmployeeEditRequest[]> {
  const { data } = await apiClient.get<EmployeeEditRequest[]>("/my/edit-requests")
  return data
}

export async function submitMyEditRequests(changes: SubmitEditRequestItem[]): Promise<EmployeeEditRequest[]> {
  const { data } = await apiClient.post<EmployeeEditRequest[]>("/my/edit-requests", { changes })
  return data
}
