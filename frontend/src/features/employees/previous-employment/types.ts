export interface PreviousEmploymentRecord {
  id: string
  employeeId: string
  companyName: string
  designation: string | null
  yearsOfExperience: number | null
  dateOfJoining: string
  dateOfLeaving: string
  reasonForLeaving: string | null
  previousUan: string | null
  relievingLetterDocumentId: string | null
  relievingLetterFileName: string | null
  lastSalarySlipDocumentId: string | null
  lastSalarySlipFileName: string | null
}

export interface PreviousEmploymentRecordWriteRequest {
  companyName: string
  designation: string | null
  yearsOfExperience: number | null
  dateOfJoining: string
  dateOfLeaving: string
  reasonForLeaving: string | null
  previousUan: string | null
}
