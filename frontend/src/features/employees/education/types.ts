export type QualificationLevel =
  | "TenthOrBelow"
  | "TwelfthOrDiploma"
  | "Graduate"
  | "PostGraduate"
  | "Doctorate"
  | "Other"

export type VerificationStatus = "Pending" | "Verified"

export interface EducationRecord {
  id: string
  employeeId: string
  qualificationLevel: QualificationLevel
  degreeName: string
  instituteName: string
  yearOfPassing: number
  specialization: string | null
  verificationStatus: VerificationStatus
  certificateDocumentId: string | null
  certificateFileName: string | null
}

export interface EducationRecordWriteRequest {
  qualificationLevel: QualificationLevel
  degreeName: string
  instituteName: string
  yearOfPassing: number
  specialization: string | null
}
