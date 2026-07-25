import type { EditableEmployeeField, EditRequestStatus } from "./types"

export const EDIT_REQUEST_STATUS_BADGE_VARIANT: Record<EditRequestStatus, "default" | "secondary" | "outline" | "destructive"> = {
  Pending: "secondary",
  Approved: "default",
  Rejected: "destructive",
}

export const EDITABLE_FIELD_LABELS: Record<EditableEmployeeField, string> = {
  PersonalEmail: "Personal Email",
  PersonalPhone: "Personal Phone",
  CurrentAddress: "Current Address",
  PermanentAddress: "Permanent Address",
  EmergencyContactName: "Emergency Contact Name",
  EmergencyContactRelation: "Emergency Contact Relation",
  EmergencyContactPhone: "Emergency Contact Phone",
}
