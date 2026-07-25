import { useQuery } from "@tanstack/react-query"
import { useState } from "react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { getInterviewerCandidates } from "./api"
import { INTERVIEW_ROUND_OPTIONS } from "./constants"
import type { InterviewRound, ScheduleInterviewRequest } from "./types"

interface ScheduleInterviewFormProps {
  onSubmit: (request: ScheduleInterviewRequest) => void
  onCancel: () => void
  isSubmitting: boolean
}

export function ScheduleInterviewForm({ onSubmit, onCancel, isSubmitting }: ScheduleInterviewFormProps) {
  const [round, setRound] = useState<InterviewRound>("InterviewRound1")
  const [scheduledAt, setScheduledAt] = useState("")
  const [durationMinutes, setDurationMinutes] = useState(45)
  const [videoLink, setVideoLink] = useState("")
  const [panelistUserIds, setPanelistUserIds] = useState<string[]>([])

  const { data: candidates = [] } = useQuery({
    queryKey: ["recruitment", "interviewer-candidates"],
    queryFn: getInterviewerCandidates,
  })

  const togglePanelist = (userId: string) => {
    setPanelistUserIds((prev) => (prev.includes(userId) ? prev.filter((id) => id !== userId) : [...prev, userId]))
  }

  const isValid = Boolean(scheduledAt) && panelistUserIds.length > 0

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault()
    if (!isValid) return
    onSubmit({
      round,
      scheduledAt: new Date(scheduledAt).toISOString(),
      durationMinutes,
      videoLink: videoLink || null,
      panelistUserIds,
    })
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-3 rounded-xl border bg-muted/30 p-3">
      <div className="grid grid-cols-2 gap-3">
        <div className="flex flex-col gap-1.5">
          <Label className="text-xs">Round</Label>
          <Select value={round} onValueChange={(value) => setRound(value as InterviewRound)}>
            <SelectTrigger className="h-9"><SelectValue /></SelectTrigger>
            <SelectContent>
              {INTERVIEW_ROUND_OPTIONS.map((option) => (
                <SelectItem key={option.value} value={option.value}>{option.label}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div className="flex flex-col gap-1.5">
          <Label className="text-xs">Duration (minutes)</Label>
          <Input
            type="number"
            min={15}
            max={240}
            className="h-9"
            value={durationMinutes}
            onChange={(e) => setDurationMinutes(Number(e.target.value) || 45)}
          />
        </div>
      </div>

      <div className="flex flex-col gap-1.5">
        <Label className="text-xs">Date &amp; time</Label>
        <Input
          type="datetime-local"
          className="h-9"
          value={scheduledAt}
          onChange={(e) => setScheduledAt(e.target.value)}
        />
      </div>

      <div className="flex flex-col gap-1.5">
        <Label className="text-xs">Video link (optional)</Label>
        <Input
          placeholder="https://meet.google.com/..."
          className="h-9"
          value={videoLink}
          onChange={(e) => setVideoLink(e.target.value)}
        />
      </div>

      <div className="flex flex-col gap-1.5">
        <Label className="text-xs">Interviewers</Label>
        <div className="flex max-h-32 flex-col gap-1 overflow-y-auto rounded-lg border bg-background p-2">
          {candidates.length === 0 ? (
            <p className="p-1 text-xs text-muted-foreground">No logins available to assign.</p>
          ) : (
            candidates.map((candidate) => (
              <label key={candidate.userId} className="flex cursor-pointer items-center gap-2 rounded px-1 py-1 text-sm hover:bg-muted">
                <input
                  type="checkbox"
                  checked={panelistUserIds.includes(candidate.userId)}
                  onChange={() => togglePanelist(candidate.userId)}
                  className="size-3.5"
                />
                {candidate.employeeName ?? candidate.email}
              </label>
            ))
          )}
        </div>
      </div>

      <div className="flex justify-end gap-2">
        <Button type="button" variant="ghost" size="sm" onClick={onCancel}>Cancel</Button>
        <Button type="submit" size="sm" disabled={!isValid || isSubmitting}>
          {isSubmitting ? "Scheduling..." : "Schedule"}
        </Button>
      </div>
    </form>
  )
}
