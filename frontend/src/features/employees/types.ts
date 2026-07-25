export type EmployeeStatus = "Active" | "OnLeave" | "NoticePeriod" | "Exited"
export type Gender = "Male" | "Female" | "Other" | "PreferNotToSay"
export type EmploymentType = "FullTime" | "Contract" | "Intern"
export type DateOfBirthProofType = "Aadhaar" | "BirthCertificate" | "TenthMarksheet" | "Other"
export type ConfirmationStatus = "Probation" | "Confirmed"

export interface Lookup {
  id: string
  name: string
}

export interface EmployeeListItem {
  id: string
  employeeCode: string
  firstName: string
  lastName: string
  legalEntityName: string
  productName: string
  department: string | null
  designation: string | null
  status: EmployeeStatus
}

export interface Employee {
  id: string
  employeeCode: string
  legalEntityId: string
  legalEntityName: string
  productId: string
  productName: string
  status: EmployeeStatus
  firstName: string
  lastName: string
  gender: Gender
  dateOfBirth: string | null
  personalEmail: string | null
  personalPhone: string | null
  currentAddress: string | null
  permanentAddress: string | null
  emergencyContactName: string | null
  emergencyContactRelation: string | null
  emergencyContactPhone: string | null
  bankAccountNumberMasked: string | null
  bankIfscCode: string | null
  dateOfJoining: string
  designation: string | null
  grade: string | null
  department: string | null
  reportingManagerId: string | null
  reportingManagerName: string | null
  employmentType: EmploymentType
  monthlyGrossSalary: number | null
  dateOfBirthProofType: DateOfBirthProofType | null
  professionalTaxState: string | null
  poshAcknowledgedAt: string | null
  isPfApplicable: boolean
  isEsicApplicable: boolean
  isMaharashtraLwfEligible: boolean
  hasMinorOrDifferentlyAbledDependent: boolean
  probationEndDate: string | null
  confirmationStatus: ConfirmationStatus
  confirmationDate: string | null
  confirmingManagerId: string | null
  confirmingManagerName: string | null
  contractStartDate: string | null
  contractEndDate: string | null
  isContractExpiringSoon: boolean
}

export interface EmployeeWriteRequest {
  legalEntityId: string
  productId: string
  firstName: string
  lastName: string
  gender: Gender
  dateOfBirth: string | null
  personalEmail: string | null
  personalPhone: string | null
  currentAddress: string | null
  permanentAddress: string | null
  emergencyContactName: string | null
  emergencyContactRelation: string | null
  emergencyContactPhone: string | null
  bankAccountNumber: string | null
  bankIfscCode: string | null
  dateOfJoining: string
  designation: string | null
  grade: string | null
  department: string | null
  reportingManagerId: string | null
  employmentType: EmploymentType
  monthlyGrossSalary: number | null
  dateOfBirthProofType: DateOfBirthProofType | null
  professionalTaxState: string | null
  probationEndDate: string | null
  contractStartDate: string | null
  contractEndDate: string | null
}

export interface ConfirmEmployeeRequest {
  confirmingManagerId: string
  confirmationDate: string | null
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
}
