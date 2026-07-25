import type { DateOfBirthProofType, EmployeeStatus, EmploymentType, Gender } from "./types"

export const GENDER_OPTIONS: { value: Gender; label: string }[] = [
  { value: "Male", label: "Male" },
  { value: "Female", label: "Female" },
  { value: "Other", label: "Other" },
  { value: "PreferNotToSay", label: "Prefer not to say" },
]

export const EMPLOYMENT_TYPE_OPTIONS: { value: EmploymentType; label: string }[] = [
  { value: "FullTime", label: "Full-time" },
  { value: "Contract", label: "Contract" },
  { value: "Intern", label: "Intern" },
]

export const EMPLOYEE_STATUS_OPTIONS: { value: EmployeeStatus; label: string }[] = [
  { value: "Active", label: "Active" },
  { value: "OnLeave", label: "On Leave" },
  { value: "NoticePeriod", label: "Notice Period" },
  { value: "Exited", label: "Exited" },
]

export const EMPLOYEE_STATUS_BADGE_VARIANT: Record<EmployeeStatus, "default" | "secondary" | "outline" | "destructive"> = {
  Active: "default",
  OnLeave: "secondary",
  NoticePeriod: "outline",
  Exited: "destructive",
}

export const DATE_OF_BIRTH_PROOF_TYPE_OPTIONS: { value: DateOfBirthProofType; label: string }[] = [
  { value: "Aadhaar", label: "Aadhaar" },
  { value: "BirthCertificate", label: "Birth Certificate" },
  { value: "TenthMarksheet", label: "10th Marksheet" },
  { value: "Other", label: "Other" },
]

// Not exhaustive of every state/UT - covers the common ones; "Other" lets HR type anything else.
export const INDIAN_STATES = [
  "Andhra Pradesh",
  "Bihar",
  "Delhi",
  "Gujarat",
  "Haryana",
  "Karnataka",
  "Kerala",
  "Madhya Pradesh",
  "Maharashtra",
  "Punjab",
  "Rajasthan",
  "Tamil Nadu",
  "Telangana",
  "Uttar Pradesh",
  "West Bengal",
  "Other",
]
