import { apiClient } from "@/lib/api-client"
import type { LoginResponse } from "@/stores/auth-store"

export interface LoginCredentials {
  email: string
  password: string
}

export async function login(credentials: LoginCredentials): Promise<LoginResponse> {
  const { data } = await apiClient.post<LoginResponse>("/auth/login", credentials)
  return data
}

export async function logout(): Promise<void> {
  await apiClient.post("/auth/logout")
}
