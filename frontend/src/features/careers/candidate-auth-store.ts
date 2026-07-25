import { create } from "zustand"

const TOKEN_KEY = "tsthrms_candidate_token"
const NAME_KEY = "tsthrms_candidate_name"

interface CandidateAuthState {
  accessToken: string | null
  candidateName: string | null
  setSession: (accessToken: string, candidateName: string) => void
  clear: () => void
}

/**
 * Deliberately localStorage-backed, unlike the staff auth-store's memory-only + HttpOnly-cookie
 * design - a candidate has no refresh-token flow (see JwtTokenGenerator.GenerateCandidateAccessToken),
 * so without some persistence here every page reload would force a fresh OTP. Acceptable given
 * the account is low-privilege and read-mostly (their own application status, nothing sensitive
 * beyond what they already submitted).
 */
export const useCandidateAuthStore = create<CandidateAuthState>((set) => ({
  accessToken: localStorage.getItem(TOKEN_KEY),
  candidateName: localStorage.getItem(NAME_KEY),
  setSession: (accessToken, candidateName) => {
    localStorage.setItem(TOKEN_KEY, accessToken)
    localStorage.setItem(NAME_KEY, candidateName)
    set({ accessToken, candidateName })
  },
  clear: () => {
    localStorage.removeItem(TOKEN_KEY)
    localStorage.removeItem(NAME_KEY)
    set({ accessToken: null, candidateName: null })
  },
}))
