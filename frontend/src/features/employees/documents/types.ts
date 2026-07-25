export type EmployeeDocumentCategory = "OfferLetter" | "PolicyAcknowledgement" | "Other"

export interface DocumentSummary {
  documentId: string
  fileName: string
  category: string
  context: string | null
  uploadedAt: string
  standaloneAttachmentId: string | null
}
