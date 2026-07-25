import { apiClient } from "@/lib/api-client"
import type { EducationRecord, EducationRecordWriteRequest, VerificationStatus } from "./types"

export async function getEducationRecords(employeeId: string): Promise<EducationRecord[]> {
  const { data } = await apiClient.get<EducationRecord[]>(`/employees/${employeeId}/education`)
  return data
}

export async function createEducationRecord(
  employeeId: string,
  request: EducationRecordWriteRequest,
): Promise<EducationRecord> {
  const { data } = await apiClient.post<EducationRecord>(`/employees/${employeeId}/education`, request)
  return data
}

export async function updateEducationRecord(
  employeeId: string,
  id: string,
  request: EducationRecordWriteRequest,
): Promise<EducationRecord> {
  const { data } = await apiClient.put<EducationRecord>(`/employees/${employeeId}/education/${id}`, request)
  return data
}

export async function deleteEducationRecord(employeeId: string, id: string): Promise<void> {
  await apiClient.delete(`/employees/${employeeId}/education/${id}`)
}

export async function updateVerificationStatus(
  employeeId: string,
  id: string,
  verificationStatus: VerificationStatus,
): Promise<EducationRecord> {
  const { data } = await apiClient.patch<EducationRecord>(
    `/employees/${employeeId}/education/${id}/verification-status`,
    { verificationStatus },
  )
  return data
}

export async function uploadCertificate(
  employeeId: string,
  id: string,
  file: File,
): Promise<EducationRecord> {
  const formData = new FormData()
  formData.append("file", file)
  const { data } = await apiClient.post<EducationRecord>(
    `/employees/${employeeId}/education/${id}/certificate`,
    formData,
    { headers: { "Content-Type": "multipart/form-data" } },
  )
  return data
}

export async function downloadDocument(documentId: string, fileName: string): Promise<void> {
  const response = await apiClient.get(`/documents/${documentId}`, { responseType: "blob" })
  const url = window.URL.createObjectURL(response.data as Blob)
  const link = window.document.createElement("a")
  link.href = url
  link.download = fileName
  link.click()
  window.URL.revokeObjectURL(url)
}
