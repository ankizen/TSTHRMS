import { apiClient } from "@/lib/api-client"
import type {
  ConfirmEmployeeRequest,
  Employee,
  EmployeeListItem,
  EmployeeStatus,
  EmployeeWriteRequest,
  Lookup,
  PagedResult,
} from "./types"

export interface EmployeeListParams {
  page?: number
  pageSize?: number
  search?: string
  status?: EmployeeStatus
}

export async function getEmployees(params: EmployeeListParams): Promise<PagedResult<EmployeeListItem>> {
  const { data } = await apiClient.get<PagedResult<EmployeeListItem>>("/employees", { params })
  return data
}

export async function getEmployee(id: string): Promise<Employee> {
  const { data } = await apiClient.get<Employee>(`/employees/${id}`)
  return data
}

export async function createEmployee(request: EmployeeWriteRequest): Promise<Employee> {
  const { data } = await apiClient.post<Employee>("/employees", request)
  return data
}

export async function updateEmployee(id: string, request: EmployeeWriteRequest): Promise<Employee> {
  const { data } = await apiClient.put<Employee>(`/employees/${id}`, request)
  return data
}

export async function updateEmployeeStatus(id: string, status: EmployeeStatus): Promise<Employee> {
  const { data } = await apiClient.patch<Employee>(`/employees/${id}/status`, { status })
  return data
}

export async function revealBankAccountNumber(id: string): Promise<string | null> {
  const { data } = await apiClient.post<{ bankAccountNumber: string | null }>(
    `/employees/${id}/reveal-bank-account`,
  )
  return data.bankAccountNumber
}

export async function acknowledgePoshPolicy(id: string): Promise<Employee> {
  const { data } = await apiClient.post<Employee>(`/employees/${id}/posh-acknowledgment`)
  return data
}

export async function confirmEmployee(id: string, request: ConfirmEmployeeRequest): Promise<Employee> {
  const { data } = await apiClient.post<Employee>(`/employees/${id}/confirm`, request)
  return data
}

export async function getLegalEntities(): Promise<Lookup[]> {
  const { data } = await apiClient.get<Lookup[]>("/legal-entities")
  return data
}

export async function getProducts(): Promise<Lookup[]> {
  const { data } = await apiClient.get<Lookup[]>("/products")
  return data
}
