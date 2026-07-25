import type { EmployeeDocumentCategory } from "./types"

export const EMPLOYEE_DOCUMENT_CATEGORY_OPTIONS: { value: EmployeeDocumentCategory; label: string }[] = [
  { value: "OfferLetter", label: "Offer Letter" },
  { value: "PolicyAcknowledgement", label: "Policy Acknowledgement" },
  { value: "Other", label: "Other" },
]
