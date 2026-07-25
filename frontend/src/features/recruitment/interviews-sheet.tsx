import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { Plus } from "lucide-react"
import { useState } from "react"
import { toast } from "sonner"
import { Button } from "@/components/ui/button"
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet"
import { getInterviews, rescheduleInterview, scheduleInterview, updateInterviewStatus } from "./api"
import { InterviewCard } from "./interview-card"
import { ScheduleInterviewForm } from "./schedule-interview-form"
import type { InterviewStatus, ScheduleInterviewRequest } from "./types"

interface InterviewsSheetProps {
  applicationId: string | null
  candidateName: string | null
  onOpenChange: (open: boolean) => void
}

export function InterviewsSheet({ applicationId, candidateName, onOpenChange }: InterviewsSheetProps) {
  const queryClient = useQueryClient()
  const [showScheduleForm, setShowScheduleForm] = useState(false)
  const queryKey = ["recruitment", "applications", applicationId, "interviews"]

  const { data: interviews = [], isLoading } = useQuery({
    queryKey,
    queryFn: () => getInterviews(applicationId!),
    enabled: Boolean(applicationId),
  })

  const invalidate = () => queryClient.invalidateQueries({ queryKey })

  const scheduleMutation = useMutation({
    mutationFn: (request: ScheduleInterviewRequest) => scheduleInterview(applicationId!, request),
    onSuccess: async () => {
      toast.success("Interview scheduled.")
      setShowScheduleForm(false)
      await invalidate()
    },
    onError: () => toast.error("Couldn't schedule the interview."),
  })

  const rescheduleMutation = useMutation({
    mutationFn: ({ interviewId, scheduledAt }: { interviewId: string; scheduledAt: string }) =>
      rescheduleInterview(interviewId, { scheduledAt }),
    onSuccess: async () => {
      toast.success("Interview rescheduled.")
      await invalidate()
    },
    onError: () => toast.error("Couldn't reschedule the interview."),
  })

  const statusMutation = useMutation({
    mutationFn: ({ interviewId, status }: { interviewId: string; status: InterviewStatus }) =>
      updateInterviewStatus(interviewId, { status }),
    onSuccess: async () => {
      toast.success("Interview updated.")
      await invalidate()
    },
    onError: () => toast.error("Couldn't update the interview."),
  })

  return (
    <Sheet open={Boolean(applicationId)} onOpenChange={onOpenChange}>
      <SheetContent className="sm:max-w-md">
        <SheetHeader>
          <SheetTitle>Interviews{candidateName ? ` - ${candidateName}` : ""}</SheetTitle>
        </SheetHeader>

        <div className="flex flex-col gap-3 overflow-y-auto px-4 pb-4">
          {isLoading ? (
            <p className="text-sm text-muted-foreground">Loading...</p>
          ) : interviews.length === 0 && !showScheduleForm ? (
            <p className="text-sm text-muted-foreground">No interviews scheduled yet.</p>
          ) : (
            interviews.map((interview) => (
              <InterviewCard
                key={interview.id}
                interview={interview}
                isMutating={rescheduleMutation.isPending || statusMutation.isPending}
                onReschedule={(scheduledAt) => rescheduleMutation.mutate({ interviewId: interview.id, scheduledAt })}
                onUpdateStatus={(status) => statusMutation.mutate({ interviewId: interview.id, status })}
              />
            ))
          )}

          {showScheduleForm ? (
            <ScheduleInterviewForm
              onSubmit={(request) => scheduleMutation.mutate(request)}
              onCancel={() => setShowScheduleForm(false)}
              isSubmitting={scheduleMutation.isPending}
            />
          ) : (
            <Button variant="outline" size="sm" className="w-fit" onClick={() => setShowScheduleForm(true)}>
              <Plus />
              Schedule interview
            </Button>
          )}
        </div>
      </SheetContent>
    </Sheet>
  )
}
