import type { AuditAction } from "./types"

export const AUDIT_ACTION_BADGE_VARIANT: Record<AuditAction, "default" | "secondary" | "outline" | "destructive"> = {
  Created: "default",
  Updated: "secondary",
  Deleted: "destructive",
  Revealed: "outline",
}

export const AUDIT_ENTITY_LABELS: Record<string, string> = {
  Employee: "Employee Profile",
  EducationRecord: "Education",
  FamilyMember: "Family Member",
  PreviousEmploymentRecord: "Previous Employment",
  IdentityDocument: "Identity Document",
  Nominee: "Nominee",
  EmployeeDocument: "Document",
}
