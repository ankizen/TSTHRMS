import { useMutation } from "@tanstack/react-query"
import { useNavigate } from "react-router-dom"
import { login as loginRequest, logout as logoutRequest } from "@/lib/auth-api"
import { useAuthStore } from "@/stores/auth-store"

export function useAuth() {
  const user = useAuthStore((s) => s.user)
  const accessToken = useAuthStore((s) => s.accessToken)
  const isInitializing = useAuthStore((s) => s.isInitializing)
  const setSession = useAuthStore((s) => s.setSession)
  const clear = useAuthStore((s) => s.clear)
  const navigate = useNavigate()

  const loginMutation = useMutation({
    mutationFn: loginRequest,
    onSuccess: (data) => {
      setSession(data)
      navigate("/", { replace: true })
    },
  })

  const logoutMutation = useMutation({
    mutationFn: logoutRequest,
    onSettled: () => {
      clear()
      navigate("/login", { replace: true })
    },
  })

  return {
    user,
    isAuthenticated: Boolean(accessToken && user),
    isInitializing,
    login: loginMutation.mutateAsync,
    isLoggingIn: loginMutation.isPending,
    loginError: loginMutation.error,
    logout: logoutMutation.mutate,
  }
}
