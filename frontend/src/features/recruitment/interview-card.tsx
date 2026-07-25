import { useState } from "react"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import {
  INTERVIEW_RECOMMENDATION_LABELS,
  INTERVIEW_ROUND_OPTIONS,
  INTERVIEW_STATUS_BADGE_VARIANT,
  INTERVIEW_STATUS_LABELS,
} from "./constants"
import type { Interview, InterviewStatus } from "./types"

interface InterviewCardProps {
  interview: Interview
  onReschedule: (scheduledAt: string) => void
  onUpdateStatus: (status: InterviewStatus) => void
  isMutating: boolean
}

export function InterviewCard({ interview, onReschedule, onUpdateStatus, isMutating }: InterviewCardProps) {
  const [rescheduling, setRescheduling] = useState(false)
  const [newScheduledAt, setNewScheduledAt] = useState("")

  const roundLabel = INTERVIEW_ROUND_OPTIONS.find((o) => o.value === interview.round)?.label ?? interview.round

  return (
    <div className="flex flex-col gap-2 rounded-xl border p-3">
      <div className="flex items-center justify-between gap-2">
        <p className="text-sm font-medium">{roundLabel}</p>
        <Badge variant={INTERVIEW_STATUS_BADGE_VARIANT[interview.status]}>
          {INTERVIEW_STATUS_LABELS[interview.status]}
        </Badge>
      </div>

      <p className="text-sm text-muted-foreground">
        {new Date(interview.scheduledAt).toLocaleString()} &middot; {interview.durationMinutes} min
        {interview.rescheduleCount > 0 && ` · rescheduled ${interview.rescheduleCount}x`}
      </p>

      {interview.videoLink && (
        <a href={interview.videoLink} target="_blank" rel="noreferrer" className="text-sm text-primary hover:underline">
          {interview.videoLink}
        </a>
      )}

      <div className="flex flex-wrap gap-1.5">
        {interview.panelists.map((panelist) => (
          <Badge key={panelist.userId} variant={panelist.hasSubmitted ? "default" : "outline"}>
            {panelist.displayName}{panelist.hasSubmitted ? " ✓" : ""}
          </Badge>
        ))}
      </div>

      {interview.visibleScorecards.length > 0 ? (
        <div className="flex flex-col gap-2 rounded-lg bg-muted/40 p-2">
          {interview.visibleScorecards.map((scorecard) => (
            <div key={scorecard.interviewerUserId} className="text-xs">
              <p className="font-medium">
                {scorecard.interviewerDisplayName} - {INTERVIEW_RECOMMENDATION_LABELS[scorecard.recommendation]}
              </p>
              <p className="text-muted-foreground">
                Technical {scorecard.technicalSkillsRating}/5 &middot; Communication {scorecard.communicationRating}/5
                &middot; Problem solving {scorecard.problemSolvingRating}/5 &middot; Culture fit {scorecard.cultureFitRating}/5
              </p>
              {scorecard.comments && <p className="mt-0.5 text-muted-foreground">{scorecard.comments}</p>}
            </div>
          ))}
        </div>
      ) : interview.panelists.length > 0 ? (
        <p className="text-xs text-muted-foreground">
          Feedback hidden until all interviewers submit.
        </p>
      ) : null}

      {rescheduling ? (
        <div className="flex items-center gap-2">
          <Input
            type="datetime-local"
            className="h-8 text-xs"
            value={newScheduledAt}
            onChange={(e) => setNewScheduledAt(e.target.value)}
          />
          <Button
            size="sm"
            disabled={!newScheduledAt || isMutating}
            onClick={() => {
              onReschedule(new Date(newScheduledAt).toISOString())
              setRescheduling(false)
            }}
          >
            Confirm
          </Button>
          <Button size="sm" variant="ghost" onClick={() => setRescheduling(false)}>Cancel</Button>
        </div>
      ) : (
        interview.status === "Scheduled" && (
          <div className="flex flex-wrap gap-1.5">
            <Button size="sm" variant="outline" onClick={() => setRescheduling(true)} disabled={isMutating}>
              Reschedule
            </Button>
            <Button size="sm" variant="outline" onClick={() => onUpdateStatus("Completed")} disabled={isMutating}>
              Mark completed
            </Button>
            <Button size="sm" variant="outline" onClick={() => onUpdateStatus("NoShow")} disabled={isMutating}>
              No-show
            </Button>
            <Button size="sm" variant="ghost" onClick={() => onUpdateStatus("Cancelled")} disabled={isMutating}>
              Cancel
            </Button>
          </div>
        )
      )}
    </div>
  )
}
