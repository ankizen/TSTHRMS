import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { AlertCircle, ArrowLeft, Star, UsersRound } from "lucide-react"
import { useState } from "react"
import { toast } from "sonner"
import { useNavigate, useParams } from "react-router-dom"
import { EmptyState } from "@/components/empty-state"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Skeleton } from "@/components/ui/skeleton"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip"
import { getApplicants, moveApplicationStage, setTalentPool } from "./api"
import { APPLICATION_STAGE_LABELS, APPLICATION_STAGE_OPTIONS } from "./constants"
import { MoveStageDialog } from "./move-stage-dialog"
import type { ApplicantListItem, ApplicationStage } from "./types"

const REASON_REQUIRED_STAGES: ApplicationStage[] = ["Rejected", "OnHold"]

export function ApplicantsPage() {
  const { jobPostingId = "" } = useParams()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const queryKey = ["recruitment", "job-postings", jobPostingId, "applicants"]

  const [pendingMove, setPendingMove] = useState<{ applicationId: string; stage: ApplicationStage } | null>(null)

  const { data: applicants, isLoading } = useQuery({
    queryKey, queryFn: () => getApplicants(jobPostingId), enabled: Boolean(jobPostingId),
  })

  const invalidate = () => queryClient.invalidateQueries({ queryKey })

  const moveMutation = useMutation({
    mutationFn: ({ applicationId, stage, reason }: { applicationId: string; stage: ApplicationStage; reason: string | null }) =>
      moveApplicationStage(applicationId, { stage, reason }),
    onSuccess: async () => {
      toast.success("Stage updated.")
      setPendingMove(null)
      await invalidate()
    },
    onError: () => toast.error("Couldn't update the stage."),
  })

  const talentPoolMutation = useMutation({
    mutationFn: ({ candidateId, isInTalentPool }: { candidateId: string; isInTalentPool: boolean }) =>
      setTalentPool(candidateId, isInTalentPool),
    onSuccess: async () => {
      toast.success("Updated talent pool tag.")
      await invalidate()
    },
    onError: () => toast.error("Couldn't update the talent pool tag."),
  })

  const handleStageChange = (applicationId: string, stage: ApplicationStage) => {
    if (REASON_REQUIRED_STAGES.includes(stage)) {
      setPendingMove({ applicationId, stage })
    } else {
      moveMutation.mutate({ applicationId, stage, reason: null })
    }
  }

  const columns = APPLICATION_STAGE_OPTIONS.map((option) => ({
    stage: option.value,
    label: option.label,
    applicants: (applicants ?? []).filter((a) => a.stage === option.value),
  }))

  return (
    <div className="flex flex-col gap-4">
      <Button variant="ghost" size="sm" className="w-fit" onClick={() => navigate(-1)}>
        <ArrowLeft />
        Back to requisition
      </Button>

      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Applicants</h1>
        <p className="text-muted-foreground">{applicants?.length ?? 0} total</p>
      </div>

      {isLoading ? (
        <div className="flex gap-4 overflow-x-auto pb-2">
          {Array.from({ length: 4 }).map((_, index) => (
            <Skeleton key={index} className="h-64 w-64 shrink-0 rounded-xl" />
          ))}
        </div>
      ) : !applicants || applicants.length === 0 ? (
        <EmptyState
          icon={UsersRound}
          title="No applicants yet"
          description="Candidates who apply from the career site will show up here."
        />
      ) : (
        <div className="flex gap-4 overflow-x-auto pb-2">
          {columns.map((column) => (
            <div key={column.stage} className="flex w-72 shrink-0 flex-col gap-3">
              <div className="flex items-center justify-between px-1">
                <p className="text-sm font-medium">{column.label}</p>
                <Badge variant="secondary">{column.applicants.length}</Badge>
              </div>
              <div className="flex flex-col gap-2">
                {column.applicants.map((applicant) => (
                  <ApplicantCard
                    key={applicant.applicationId}
                    applicant={applicant}
                    onStageChange={(stage) => handleStageChange(applicant.applicationId, stage)}
                    onToggleTalentPool={() =>
                      talentPoolMutation.mutate({
                        candidateId: applicant.candidateId,
                        isInTalentPool: !applicant.isInTalentPool,
                      })
                    }
                  />
                ))}
              </div>
            </div>
          ))}
        </div>
      )}

      <MoveStageDialog
        open={Boolean(pendingMove)}
        onOpenChange={(open) => !open && setPendingMove(null)}
        onConfirm={(reason) => pendingMove && moveMutation.mutate({ ...pendingMove, reason })}
        isSubmitting={moveMutation.isPending}
        targetStage={pendingMove?.stage ?? null}
      />
    </div>
  )
}

function ApplicantCard({
  applicant, onStageChange, onToggleTalentPool,
}: {
  applicant: ApplicantListItem
  onStageChange: (stage: ApplicationStage) => void
  onToggleTalentPool: () => void
}) {
  return (
    <div className="flex flex-col gap-2 rounded-xl border bg-card p-3 shadow-sm">
      <div className="flex items-start justify-between gap-2">
        <div>
          <p className="text-sm font-medium">{applicant.firstName} {applicant.lastName}</p>
          <p className="text-xs text-muted-foreground">{applicant.email}</p>
          <p className="text-xs text-muted-foreground">{applicant.phone}</p>
        </div>
        <Button type="button" variant="ghost" size="icon" className="size-7 shrink-0" onClick={onToggleTalentPool}>
          <Star className={applicant.isInTalentPool ? "size-4 fill-amber-400 text-amber-400" : "size-4"} />
        </Button>
      </div>

      <div className="flex flex-wrap items-center gap-1.5">
        <Badge variant="secondary">{applicant.source}</Badge>
        {applicant.otherApplications.length > 0 && (
          <Tooltip>
            <TooltipTrigger asChild>
              <span className="flex items-center gap-1 rounded-full bg-amber-500/10 px-2 py-0.5 text-xs font-medium text-amber-600 dark:text-amber-400">
                <AlertCircle className="size-3" />
                {applicant.otherApplications.length} other application{applicant.otherApplications.length === 1 ? "" : "s"}
              </span>
            </TooltipTrigger>
            <TooltipContent className="max-w-56">
              <div className="flex flex-col gap-1">
                {applicant.otherApplications.map((other) => (
                  <span key={other.applicationId}>
                    {other.jobPostingTitle} - {APPLICATION_STAGE_LABELS[other.stage]}
                  </span>
                ))}
              </div>
            </TooltipContent>
          </Tooltip>
        )}
      </div>

      <div className="text-xs text-muted-foreground">
        CTC: {applicant.currentCtc ?? "-"} / {applicant.expectedCtc ?? "-"} &middot; Notice: {applicant.noticePeriodDays ?? "-"}d
      </div>

      {applicant.resumeDocumentId && (
        <a
          href={`${import.meta.env.VITE_API_URL || "/api"}/documents/${applicant.resumeDocumentId}`}
          target="_blank"
          rel="noreferrer"
          className="text-xs text-primary hover:underline"
        >
          View resume
        </a>
      )}

      {applicant.stage === "Rejected" && applicant.rejectionReason && (
        <p className="text-xs text-muted-foreground">Reason: {applicant.rejectionReason}</p>
      )}

      <Select value={applicant.stage} onValueChange={(value) => onStageChange(value as ApplicationStage)}>
        <SelectTrigger className="h-8 w-full text-xs">
          <SelectValue />
        </SelectTrigger>
        <SelectContent>
          {APPLICATION_STAGE_OPTIONS.map((option) => (
            <SelectItem key={option.value} value={option.value}>{option.label}</SelectItem>
          ))}
        </SelectContent>
      </Select>
    </div>
  )
}
