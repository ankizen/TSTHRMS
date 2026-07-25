import axios, { type InternalAxiosRequestConfig } from "axios"
import type { LoginResponse } from "@/stores/auth-store"
import { useAuthStore } from "@/stores/auth-store"

interface RetryableRequestConfig extends InternalAxiosRequestConfig {
  _retry?: boolean
}

// Same-origin deployments (Vite dev proxy, or the API serving the built SPA from wwwroot) never
// need this set - only a split deployment (frontend on Vercel, API elsewhere, e.g. Coolify) does.
const API_BASE_URL = import.meta.env.VITE_API_URL || "/api"

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  withCredentials: true, // send the HttpOnly refresh cookie
})

apiClient.interceptors.request.use((config) => {
  const token = useAuthStore.getState().accessToken
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

let refreshPromise: Promise<string | null> | null = null

export async function refreshAccessToken(): Promise<string | null> {
  refreshPromise ??= (async () => {
    try {
      const { data } = await axios.post<LoginResponse>(
        `${API_BASE_URL}/auth/refresh`,
        {},
        { withCredentials: true },
      )
      useAuthStore.getState().setSession(data)
      return data.accessToken
    } catch {
      useAuthStore.getState().clear()
      return null
    } finally {
      refreshPromise = null
    }
  })()

  return refreshPromise
}

apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config as RetryableRequestConfig | undefined

    if (error.response?.status === 401 && originalRequest && !originalRequest._retry) {
      originalRequest._retry = true
      const newToken = await refreshAccessToken()
      if (newToken) {
        originalRequest.headers.Authorization = `Bearer ${newToken}`
        return apiClient(originalRequest)
      }
    }

    return Promise.reject(error)
  },
)
