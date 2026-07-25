import { apiClient } from "@/lib/api-client"
import type { PreviousEmploymentRecord, PreviousEmploymentRecordWriteRequest } from "./types"

export async function getPreviousEmploymentRecords(employeeId: string): Promise<PreviousEmploymentRecord[]> {
  const { data } = await apiClient.get<PreviousEmploymentRecord[]>(`/employees/${employeeId}/previous-employment`)
  return data
}

export async function createPreviousEmploymentRecord(
  employeeId: string,
  request: PreviousEmploymentRecordWriteRequest,
): Promise<PreviousEmploymentRecord> {
  const { data } = await apiClient.post<PreviousEmploymentRecord>(
    `/employees/${employeeId}/previous-employment`,
    request,
  )
  return data
}

export async function updatePreviousEmploymentRecord(
  employeeId: string,
  id: string,
  request: PreviousEmploymentRecordWriteRequest,
): Promise<PreviousEmploymentRecord> {
  const { data } = await apiClient.put<PreviousEmploymentRecord>(
    `/employees/${employeeId}/previous-employment/${id}`,
    request,
  )
  return data
}

export async function deletePreviousEmploymentRecord(employeeId: string, id: string): Promise<void> {
  await apiClient.delete(`/employees/${employeeId}/previous-employment/${id}`)
}

async function uploadDocument(
  employeeId: string,
  id: string,
  slot: "relieving-letter" | "salary-slip",
  file: File,
): Promise<PreviousEmploymentRecord> {
  const formData = new FormData()
  formData.append("file", file)
  const { data } = await apiClient.post<PreviousEmploymentRecord>(
    `/employees/${employeeId}/previous-employment/${id}/${slot}`,
    formData,
    { headers: { "Content-Type": "multipart/form-data" } },
  )
  return data
}

export const uploadRelievingLetter = (employeeId: string, id: string, file: File) =>
  uploadDocument(employeeId, id, "relieving-letter", file)

export const uploadSalarySlip = (employeeId: string, id: string, file: File) =>
  uploadDocument(employeeId, id, "salary-slip", file)
