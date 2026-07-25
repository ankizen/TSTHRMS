import { apiClient } from "@/lib/api-client"
import type { Nominee, NomineeWriteRequest } from "./types"

export async function getNominees(employeeId: string): Promise<Nominee[]> {
  const { data } = await apiClient.get<Nominee[]>(`/employees/${employeeId}/nominees`)
  return data
}

export async function createNominee(employeeId: string, request: NomineeWriteRequest): Promise<Nominee> {
  const { data } = await apiClient.post<Nominee>(`/employees/${employeeId}/nominees`, request)
  return data
}

export async function updateNominee(
  employeeId: string,
  id: string,
  request: NomineeWriteRequest,
): Promise<Nominee> {
  const { data } = await apiClient.put<Nominee>(`/employees/${employeeId}/nominees/${id}`, request)
  return data
}

export async function deleteNominee(employeeId: string, id: string): Promise<void> {
  await apiClient.delete(`/employees/${employeeId}/nominees/${id}`)
}

export async function uploadNomineeConsentDocument(
  employeeId: string,
  id: string,
  file: File,
): Promise<Nominee> {
  const formData = new FormData()
  formData.append("file", file)
  const { data } = await apiClient.post<Nominee>(
    `/employees/${employeeId}/nominees/${id}/consent-document`,
    formData,
    { headers: { "Content-Type": "multipart/form-data" } },
  )
  return data
}
