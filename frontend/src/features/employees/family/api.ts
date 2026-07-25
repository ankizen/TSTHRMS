import { apiClient } from "@/lib/api-client"
import type { FamilyMember, FamilyMemberWriteRequest } from "./types"

export async function getFamilyMembers(employeeId: string): Promise<FamilyMember[]> {
  const { data } = await apiClient.get<FamilyMember[]>(`/employees/${employeeId}/family`)
  return data
}

export async function createFamilyMember(
  employeeId: string,
  request: FamilyMemberWriteRequest,
): Promise<FamilyMember> {
  const { data } = await apiClient.post<FamilyMember>(`/employees/${employeeId}/family`, request)
  return data
}

export async function updateFamilyMember(
  employeeId: string,
  id: string,
  request: FamilyMemberWriteRequest,
): Promise<FamilyMember> {
  const { data } = await apiClient.put<FamilyMember>(`/employees/${employeeId}/family/${id}`, request)
  return data
}

export async function deleteFamilyMember(employeeId: string, id: string): Promise<void> {
  await apiClient.delete(`/employees/${employeeId}/family/${id}`)
}
