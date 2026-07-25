import { useMutation, useQuery } from "@tanstack/react-query"
import axios from "axios"
import { CheckCircle2, Clock } from "lucide-react"
import { useState } from "react"
import { useParams } from "react-router-dom"
import { Button } from "@/components/ui/button"
import { Skeleton } from "@/components/ui/skeleton"
import { Textarea } from "@/components/ui/textarea"
import { getPublicAssessment, submitPublicAssessment } from "./api"
import { ASSESSMENT_TYPE_LABELS } from "./constants"

export function AssessmentPage() {
  const { tenantSlug = "", token = "" } = useParams()
  const [submissionText, setSubmissionText] = useState("")

  const { data: assessment, isLoading, error } = useQuery({
    queryKey: ["careers", tenantSlug, "assessment", token],
    queryFn: () => getPublicAssessment(tenantSlug, token),
    enabled: Boolean(tenantSlug && token),
    retry: false,
  })

  const submitMutation = useMutation({
    mutationFn: () => submitPublicAssessment(tenantSlug, token, submissionText),
  })

  if (isLoading) {
    return (
      <div className="flex flex-col gap-4">
        <Skeleton className="h-8 w-2/3" />
        <Skeleton className="h-48 w-full" />
      </div>
    )
  }

  if (error || !assessment) {
    return (
      <div className="flex flex-col items-center gap-3 py-16 text-center">
        <h1 className="font-heading text-2xl font-semibold">This link isn't valid</h1>
        <p className="text-muted-foreground">It may have expired or the link was copied incorrectly.</p>
      </div>
    )
  }

  const isDone = assessment.alreadySubmitted || submitMutation.isSuccess

  if (isDone) {
    return (
      <div className="flex flex-col items-center gap-3 py-16 text-center">
        <CheckCircle2 className="size-10 text-emerald-500" />
        <h1 className="font-heading text-2xl font-semibold">Submission received</h1>
        <p className="max-w-sm text-muted-foreground">
          Thanks for completing the assessment for {assessment.jobTitle}. Our hiring team will follow up on next steps.
        </p>
      </div>
    )
  }

  if (assessment.isExpired) {
    return (
      <div className="flex flex-col items-center gap-3 py-16 text-center">
        <h1 className="font-heading text-2xl font-semibold">This assessment has expired</h1>
        <p className="max-w-sm text-muted-foreground">
          The response window for {assessment.jobTitle} has closed. Reach out to the hiring team if you believe this
          is a mistake.
        </p>
      </div>
    )
  }

  const submitError = axios.isAxiosError<{ error?: string }>(submitMutation.error)
    ? submitMutation.error.response?.data?.error ?? "Something went wrong. Please try again."
    : submitMutation.error
      ? "Something went wrong. Please try again."
      : null

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-2">
        <h1 className="font-heading text-2xl font-semibold tracking-tight">{assessment.jobTitle}</h1>
        <p className="text-sm text-muted-foreground">{ASSESSMENT_TYPE_LABELS[assessment.type]}</p>
        <div className="flex items-center gap-1.5 text-sm text-muted-foreground">
          <Clock className="size-3.5" />
          {assessment.timeLimitMinutes} minutes once you begin - submit by {new Date(assessment.dueAt).toLocaleString()}
        </div>
      </div>

      {assessment.instructions && (
        <div className="rounded-xl border bg-muted/30 p-4 leading-relaxed whitespace-pre-wrap">
          {assessment.instructions}
        </div>
      )}

      <form
        onSubmit={(event) => {
          event.preventDefault()
          submitMutation.mutate()
        }}
        className="flex flex-col gap-3"
      >
        <Textarea
          rows={12}
          placeholder="Write or paste your response here..."
          value={submissionText}
          onChange={(event) => setSubmissionText(event.target.value)}
        />
        {submitError && (
          <p className="rounded-lg bg-destructive/10 px-3 py-2 text-sm text-destructive">{submitError}</p>
        )}
        <Button type="submit" className="w-fit" disabled={!submissionText.trim() || submitMutation.isPending}>
          {submitMutation.isPending ? "Submitting..." : "Submit response"}
        </Button>
      </form>
    </div>
  )
}
