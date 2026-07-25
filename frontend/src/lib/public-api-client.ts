import axios from "axios"

// The career site is anonymous - no auth token, no refresh-on-401 retry loop like apiClient has.
const API_BASE_URL = import.meta.env.VITE_API_URL || "/api"

export const publicApiClient = axios.create({
  baseURL: API_BASE_URL,
})
