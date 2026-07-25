import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { CalendarClock } from "lucide-react"
import { useState } from "react"
import { toast } from "sonner"
import { EmptyState } from "@/components/empty-state"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Skeleton } from "@/components/ui/skeleton"
import { getMyInterviews, submitScorecard } from "./api"
import { INTERVIEW_ROUND_OPTIONS, INTERVIEW_STATUS_BADGE_VARIANT, INTERVIEW_STATUS_LABELS } from "./constants"
import { SubmitScorecardDialog } from "./submit-scorecard-dialog"
import type { MyInterview, SubmitScorecardRequest } from "./types"

export function MyInterviewsPage() {
  const queryClient = useQueryClient()
  const queryKey = ["recruitment", "my-interviews"]
  const [pendingScorecard, setPendingScorecard] = useState<MyInterview | null>(null)

  const { data: interviews = [], isLoading } = useQuery({ queryKey, queryFn: getMyInterviews })

  const scorecardMutation = useMutation({
    mutationFn: (request: SubmitScorecardRequest) => submitScorecard(pendingScorecard!.interviewId, request),
    onSuccess: async () => {
      toast.success("Feedback submitted.")
      setPendingScorecard(null)
      await queryClient.invalidateQueries({ queryKey })
    },
    onError: () => toast.error("Couldn't submit feedback - it may already be recorded."),
  })

  return (
    <div className="flex flex-col gap-4">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">My Interviews</h1>
        <p className="text-muted-foreground">Interviews you've been assigned to as a panelist</p>
      </div>

      {isLoading ? (
        <div className="flex flex-col gap-3">
          {Array.from({ length: 3 }).map((_, index) => (
            <Skeleton key={index} className="h-24 w-full rounded-xl" />
          ))}
        </div>
      ) : interviews.length === 0 ? (
        <EmptyState
          icon={CalendarClock}
          title="No interviews assigned"
          description="Interviews you're asked to panel will show up here."
        />
      ) : (
        <div className="flex flex-col gap-3">
          {interviews.map((interview) => (
            <div key={interview.interviewId} className="flex flex-col gap-2 rounded-xl border p-4 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <p className="font-medium">{interview.candidateName} - {interview.jobPostingTitle}</p>
                <p className="text-sm text-muted-foreground">
                  {INTERVIEW_ROUND_OPTIONS.find((o) => o.value === interview.round)?.label ?? interview.round}
                  {" · "}
                  {new Date(interview.scheduledAt).toLocaleString()} ({interview.durationMinutes} min)
                </p>
                {interview.videoLink && (
                  <a href={interview.videoLink} target="_blank" rel="noreferrer" className="text-sm text-primary hover:underline">
                    {interview.videoLink}
                  </a>
                )}
              </div>
              <div className="flex items-center gap-2">
                <Badge variant={INTERVIEW_STATUS_BADGE_VARIANT[interview.status]}>
                  {INTERVIEW_STATUS_LABELS[interview.status]}
                </Badge>
                {interview.hasSubmitted ? (
                  <Badge variant="secondary">Feedback submitted</Badge>
                ) : (
                  <Button size="sm" onClick={() => setPendingScorecard(interview)}>
                    Submit feedback
                  </Button>
                )}
              </div>
            </div>
          ))}
        </div>
      )}

      <SubmitScorecardDialog
        open={Boolean(pendingScorecard)}
        onOpenChange={(open) => !open && setPendingScorecard(null)}
        onSubmit={(request) => scorecardMutation.mutate(request)}
        isSubmitting={scorecardMutation.isPending}
        candidateName={pendingScorecard?.candidateName ?? null}
      />
    </div>
  )
}
