export type NominationType = "ProvidentFund" | "Gratuity" | "Insurance"

export interface Nominee {
  id: string
  employeeId: string
  nominationType: NominationType
  name: string
  relation: string
  sharePercentage: number | null
  contactNumber: string | null
  familyMemberId: string | null
  familyMemberName: string | null
  consentDocumentId: string | null
  consentFileName: string | null
}

export interface NomineeWriteRequest {
  nominationType: NominationType
  name: string
  relation: string
  sharePercentage: number | null
  contactNumber: string | null
  familyMemberId: string | null
}
