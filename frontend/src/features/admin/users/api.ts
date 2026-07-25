import { apiClient } from "@/lib/api-client"
import type { CreateUserRequest, UserSummary } from "./types"

export async function getUsers(): Promise<UserSummary[]> {
  const { data } = await apiClient.get<UserSummary[]>("/users")
  return data
}

export async function createUser(request: CreateUserRequest): Promise<UserSummary> {
  const { data } = await apiClient.post<UserSummary>("/users", request)
  return data
}

export async function deleteUser(id: string): Promise<void> {
  await apiClient.delete(`/users/${id}`)
}
