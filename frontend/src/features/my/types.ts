import type { EmployeeStatus } from "@/features/employees/types"

export type EditableEmployeeField =
  | "PersonalEmail"
  | "PersonalPhone"
  | "CurrentAddress"
  | "PermanentAddress"
  | "EmergencyContactName"
  | "EmergencyContactRelation"
  | "EmergencyContactPhone"

export type EditRequestStatus = "Pending" | "Approved" | "Rejected"

export interface DirectReportSummary {
  id: string
  employeeCode: string
  firstName: string
  lastName: string
  designation: string | null
  department: string | null
  workLocation: string | null
  status: EmployeeStatus
  dateOfJoining: string
}

export interface EmployeeEditRequest {
  id: string
  employeeId: string
  employeeName: string
  field: EditableEmployeeField
  oldValue: string | null
  newValue: string
  status: EditRequestStatus
  reviewedByUserId: string | null
  reviewedByDisplayName: string | null
  reviewedAt: string | null
  reviewNote: string | null
  createdAt: string
}

export interface SubmitEditRequestItem {
  field: EditableEmployeeField
  newValue: string | null
}
