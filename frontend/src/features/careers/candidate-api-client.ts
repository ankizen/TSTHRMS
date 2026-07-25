import axios from "axios"
import { useCandidateAuthStore } from "./candidate-auth-store"

const API_BASE_URL = import.meta.env.VITE_API_URL || "/api"

export const candidateApiClient = axios.create({ baseURL: API_BASE_URL })

candidateApiClient.interceptors.request.use((config) => {
  const token = useCandidateAuthStore.getState().accessToken
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

candidateApiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      useCandidateAuthStore.getState().clear()
    }
    return Promise.reject(error)
  },
)
