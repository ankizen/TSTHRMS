export type FamilyRelation = "Spouse" | "Parent" | "Child" | "Other"

export interface FamilyMember {
  id: string
  employeeId: string
  relation: FamilyRelation
  name: string
  dateOfBirth: string | null
  isDependent: boolean
  isDifferentlyAbled: boolean
}

export interface FamilyMemberWriteRequest {
  relation: FamilyRelation
  name: string
  dateOfBirth: string | null
  isDependent: boolean
  isDifferentlyAbled: boolean
}
