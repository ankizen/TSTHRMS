import { apiClient } from "@/lib/api-client"

/** Downloads a Document (see backend DocumentsController) via an authenticated request -
 * a plain <a href> wouldn't carry the Bearer token, since that only travels through axios. */
export async function downloadDocument(documentId: string, fileName: string): Promise<void> {
  const response = await apiClient.get(`/documents/${documentId}`, { responseType: "blob" })
  const url = window.URL.createObjectURL(response.data as Blob)
  const link = window.document.createElement("a")
  link.href = url
  link.download = fileName
  link.click()
  window.URL.revokeObjectURL(url)
}
