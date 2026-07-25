export interface UserSummary {
  id: string
  email: string
  roles: string[]
  employeeId: string | null
  employeeName: string | null
  assignedLegalEntityId: string | null
  assignedLegalEntityName: string | null
  assignedProductId: string | null
  assignedProductName: string | null
}

export interface CreateUserRequest {
  employeeId: string
  email: string
  password: string
  role: string
  assignedLegalEntityId: string | null
  assignedProductId: string | null
}
