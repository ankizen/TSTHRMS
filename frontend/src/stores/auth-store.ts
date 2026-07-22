import { create } from "zustand"

export interface AuthUser {
  id: string
  email: string
  tenantId: string
  roles: string[]
}

export interface LoginResponse {
  accessToken: string
  accessTokenExpiresAt: string
  user: AuthUser
}

interface AuthState {
  accessToken: string | null
  user: AuthUser | null
  /** True until the initial silent-refresh attempt on app boot resolves. */
  isInitializing: boolean
  setSession: (data: LoginResponse) => void
  clear: () => void
}

/**
 * Deliberately in-memory only (no localStorage/sessionStorage persistence) - the access
 * token is short-lived and the refresh token lives in an HttpOnly cookie the JS layer
 * never touches, so an XSS payload can't exfiltrate a long-lived credential from here.
 */
export const useAuthStore = create<AuthState>((set) => ({
  accessToken: null,
  user: null,
  isInitializing: true,
  setSession: (data) =>
    set({ accessToken: data.accessToken, user: data.user, isInitializing: false }),
  clear: () => set({ accessToken: null, user: null, isInitializing: false }),
}))
