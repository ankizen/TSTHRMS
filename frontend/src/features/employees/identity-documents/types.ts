export type IdentityDocumentType = "Pan" | "Aadhaar" | "Passport" | "Uan" | "Esic"

export interface IdentityDocument {
  id: string
  employeeId: string
  documentType: IdentityDocumentType
  numberDisplay: string
  expiryDate: string | null
  proofDocumentId: string | null
  proofFileName: string | null
}

export interface IdentityDocumentWriteRequest {
  documentType: IdentityDocumentType
  number: string
  expiryDate: string | null
}
