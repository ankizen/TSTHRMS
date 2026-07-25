import { useMutation } from "@tanstack/react-query"
import { useState } from "react"
import { useNavigate, useParams } from "react-router-dom"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { requestCandidateOtp, verifyCandidateOtp } from "./api"
import { useCandidateAuthStore } from "./candidate-auth-store"

export function CandidateLoginPage() {
  const { tenantSlug = "" } = useParams()
  const navigate = useNavigate()
  const setSession = useCandidateAuthStore((s) => s.setSession)

  const [email, setEmail] = useState("")
  const [code, setCode] = useState("")
  const [codeSent, setCodeSent] = useState(false)

  const requestMutation = useMutation({
    mutationFn: () => requestCandidateOtp(tenantSlug, email),
    onSuccess: () => setCodeSent(true),
  })

  const verifyMutation = useMutation({
    mutationFn: () => verifyCandidateOtp(tenantSlug, email, code),
    onSuccess: (result) => {
      if (result.succeeded && result.accessToken && result.candidateName) {
        setSession(result.accessToken, result.candidateName)
        navigate(`/careers/${tenantSlug}/portal`)
      }
    },
  })

  return (
    <div className="mx-auto flex max-w-sm flex-col gap-6 py-12">
      <div className="flex flex-col gap-1.5">
        <h1 className="font-heading text-2xl font-semibold tracking-tight">Track your application</h1>
        <p className="text-sm text-muted-foreground">
          {codeSent
            ? "Enter the code we emailed you to sign in."
            : "Enter the email you applied with and we'll send you a sign-in code."}
        </p>
      </div>

      {!codeSent ? (
        <form
          onSubmit={(event) => {
            event.preventDefault()
            requestMutation.mutate()
          }}
          className="flex flex-col gap-3"
        >
          <div className="flex flex-col gap-2">
            <Label htmlFor="candidateEmail">Email</Label>
            <Input
              id="candidateEmail"
              type="email"
              autoFocus
              value={email}
              onChange={(event) => setEmail(event.target.value)}
            />
          </div>
          <Button type="submit" disabled={!email || requestMutation.isPending}>
            {requestMutation.isPending ? "Sending..." : "Send sign-in code"}
          </Button>
        </form>
      ) : (
        <form
          onSubmit={(event) => {
            event.preventDefault()
            verifyMutation.mutate()
          }}
          className="flex flex-col gap-3"
        >
          <div className="flex flex-col gap-2">
            <Label htmlFor="candidateCode">6-digit code</Label>
            <Input
              id="candidateCode"
              autoFocus
              maxLength={6}
              value={code}
              onChange={(event) => setCode(event.target.value)}
            />
          </div>
          {verifyMutation.isError && (
            <p className="rounded-lg bg-destructive/10 px-3 py-2 text-sm text-destructive">
              That code is invalid or has expired.
            </p>
          )}
          {verifyMutation.isSuccess && !verifyMutation.data.succeeded && (
            <p className="rounded-lg bg-destructive/10 px-3 py-2 text-sm text-destructive">
              That code is invalid or has expired.
            </p>
          )}
          <Button type="submit" disabled={code.length !== 6 || verifyMutation.isPending}>
            {verifyMutation.isPending ? "Verifying..." : "Sign in"}
          </Button>
          <button
            type="button"
            className="text-xs text-muted-foreground hover:underline"
            onClick={() => setCodeSent(false)}
          >
            Use a different email
          </button>
        </form>
      )}
    </div>
  )
}
