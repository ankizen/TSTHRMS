import { apiClient } from "@/lib/api-client"
import type { BulkImportSummary } from "./types"

export async function downloadBulkImportTemplate(): Promise<void> {
  const response = await apiClient.get("/employees/bulk-import/template", { responseType: "blob" })
  const url = window.URL.createObjectURL(response.data as Blob)
  const link = window.document.createElement("a")
  link.href = url
  link.download = "employee-bulk-import-template.xlsx"
  link.click()
  window.URL.revokeObjectURL(url)
}

function toFormData(file: File): FormData {
  const formData = new FormData()
  formData.append("file", file)
  return formData
}

export async function validateBulkImport(file: File): Promise<BulkImportSummary> {
  const { data } = await apiClient.post<BulkImportSummary>("/employees/bulk-import/validate", toFormData(file), {
    headers: { "Content-Type": "multipart/form-data" },
  })
  return data
}

export async function commitBulkImport(file: File): Promise<BulkImportSummary> {
  const { data } = await apiClient.post<BulkImportSummary>("/employees/bulk-import/commit", toFormData(file), {
    headers: { "Content-Type": "multipart/form-data" },
  })
  return data
}
