import type { EmployeeStatus, EmploymentType, Gender } from "./types"

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
