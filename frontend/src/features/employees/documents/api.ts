import { apiClient } from "@/lib/api-client"
import type { DocumentSummary, EmployeeDocumentCategory } from "./types"

export async function getEmployeeDocuments(employeeId: string): Promise<DocumentSummary[]> {
  const { data } = await apiClient.get<DocumentSummary[]>(`/employees/${employeeId}/documents`)
  return data
}

export async function uploadEmployeeDocument(
  employeeId: string,
  category: EmployeeDocumentCategory,
  notes: string | null,
  file: File,
): Promise<DocumentSummary> {
  const formData = new FormData()
  formData.append("category", category)
  if (notes) {
    formData.append("notes", notes)
  }
  formData.append("file", file)

  const { data } = await apiClient.post<DocumentSummary>(`/employees/${employeeId}/documents`, formData, {
    headers: { "Content-Type": "multipart/form-data" },
  })
  return data
}

export async function deleteEmployeeDocument(employeeId: string, standaloneAttachmentId: string): Promise<void> {
  await apiClient.delete(`/employees/${employeeId}/documents/${standaloneAttachmentId}`)
}
