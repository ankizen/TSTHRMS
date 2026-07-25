import type { IdentityDocumentType } from "./types"

export const IDENTITY_DOCUMENT_TYPE_OPTIONS: { value: IdentityDocumentType; label: string }[] = [
  { value: "Pan", label: "PAN" },
  { value: "Aadhaar", label: "Aadhaar" },
  { value: "Passport", label: "Passport" },
  { value: "Uan", label: "UAN" },
  { value: "Esic", label: "ESIC Number" },
]

export const IDENTITY_DOCUMENT_TYPE_LABEL: Record<IdentityDocumentType, string> = Object.fromEntries(
  IDENTITY_DOCUMENT_TYPE_OPTIONS.map((option) => [option.value, option.label]),
) as Record<IdentityDocumentType, string>
