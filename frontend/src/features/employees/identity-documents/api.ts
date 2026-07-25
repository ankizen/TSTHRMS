import { apiClient } from "@/lib/api-client"
import type { IdentityDocument, IdentityDocumentWriteRequest } from "./types"

export async function getIdentityDocuments(employeeId: string): Promise<IdentityDocument[]> {
  const { data } = await apiClient.get<IdentityDocument[]>(`/employees/${employeeId}/identity-documents`)
  return data
}

export async function createIdentityDocument(
  employeeId: string,
  request: IdentityDocumentWriteRequest,
): Promise<IdentityDocument> {
  const { data } = await apiClient.post<IdentityDocument>(`/employees/${employeeId}/identity-documents`, request)
  return data
}

export async function updateIdentityDocument(
  employeeId: string,
  id: string,
  request: IdentityDocumentWriteRequest,
): Promise<IdentityDocument> {
  const { data } = await apiClient.put<IdentityDocument>(`/employees/${employeeId}/identity-documents/${id}`, request)
  return data
}

export async function deleteIdentityDocument(employeeId: string, id: string): Promise<void> {
  await apiClient.delete(`/employees/${employeeId}/identity-documents/${id}`)
}

export async function uploadIdentityDocumentProof(
  employeeId: string,
  id: string,
  file: File,
): Promise<IdentityDocument> {
  const formData = new FormData()
  formData.append("file", file)
  const { data } = await apiClient.post<IdentityDocument>(
    `/employees/${employeeId}/identity-documents/${id}/proof`,
    formData,
    { headers: { "Content-Type": "multipart/form-data" } },
  )
  return data
}

export async function revealIdentityDocumentNumber(employeeId: string, id: string): Promise<string> {
  const { data } = await apiClient.post<{ number: string }>(
    `/employees/${employeeId}/identity-documents/${id}/reveal`,
  )
  return data.number
}
