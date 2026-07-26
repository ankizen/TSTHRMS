import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { AlertCircle, ArrowLeft, Banknote, CalendarClock, ClipboardCheck, FileCheck2, ShieldCheck, Star, UsersRound } from "lucide-react"
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
import {
  getApplicants, getAssessmentDetail, getTestConfiguration, moveApplicationStage,
  scoreAssessment, sendAssessment, setTalentPool,
} from "./api"
import { BgvSheet } from "./bgv-sheet"
import { APPLICATION_STAGE_LABELS, APPLICATION_STAGE_OPTIONS } from "./constants"
import { InterviewsSheet } from "./interviews-sheet"
import { MoveStageDialog } from "./move-stage-dialog"
import { OfferSheet } from "./offer-sheet"
import { PreboardingSheet } from "./preboarding-sheet"
import { ScoreAssessmentDialog } from "./score-assessment-dialog"
import type { ApplicantListItem, ApplicationStage, AssessmentSummary } from "./types"

const REASON_REQUIRED_STAGES: ApplicationStage[] = ["Rejected", "OnHold"]

export function ApplicantsPage() {
  const { jobPostingId = "" } = useParams()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const queryKey = ["recruitment", "job-postings", jobPostingId, "applicants"]

  const [pendingMove, setPendingMove] = useState<{ applicationId: string; stage: ApplicationStage } | null>(null)
  const [interviewsFor, setInterviewsFor] = useState<{ applicationId: string; candidateName: string } | null>(null)
  const [offerFor, setOfferFor] = useState<{ applicationId: string; candidateName: string } | null>(null)
  const [bgvFor, setBgvFor] = useState<{ applicationId: string; candidateName: string } | null>(null)
  const [preboardingFor, setPreboardingFor] = useState<{ applicationId: string; candidateName: string } | null>(null)
  const [scoringAssessment, setScoringAssessment] = useState<AssessmentSummary | null>(null)

  const { data: applicants, isLoading } = useQuery({
    queryKey, queryFn: () => getApplicants(jobPostingId), enabled: Boolean(jobPostingId),
  })

  const { data: testConfiguration } = useQuery({
    queryKey: ["recruitment", "job-postings", jobPostingId, "test-configuration"],
    queryFn: () => getTestConfiguration(jobPostingId),
    enabled: Boolean(jobPostingId),
  })

  const { data: assessmentDetail } = useQuery({
    queryKey: ["recruitment", "assessments", scoringAssessment?.id],
    queryFn: () => getAssessmentDetail(scoringAssessment!.id),
    enabled: Boolean(scoringAssessment),
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

  const sendAssessmentMutation = useMutation({
    mutationFn: (applicationId: string) => sendAssessment(applicationId),
    onSuccess: async (result) => {
      if (result.succeeded) {
        toast.success("Assessment sent to the candidate.")
        await invalidate()
      } else {
        toast.error(result.error ?? "Couldn't send the assessment.")
      }
    },
    onError: () => toast.error("Couldn't send the assessment."),
  })

  const scoreMutation = useMutation({
    mutationFn: ({ assessmentSubmissionId, score, comments }: { assessmentSubmissionId: string; score: number; comments: string | null }) =>
      scoreAssessment(assessmentSubmissionId, { score, comments }),
    onSuccess: async () => {
      toast.success("Score saved.")
      setScoringAssessment(null)
      await invalidate()
    },
    onError: () => toast.error("Couldn't save the score."),
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
                    isAssessmentEnabled={testConfiguration?.isEnabled ?? false}
                    onStageChange={(stage) => handleStageChange(applicant.applicationId, stage)}
                    onToggleTalentPool={() =>
                      talentPoolMutation.mutate({
                        candidateId: applicant.candidateId,
                        isInTalentPool: !applicant.isInTalentPool,
                      })
                    }
                    onOpenInterviews={() =>
                      setInterviewsFor({
                        applicationId: applicant.applicationId,
                        candidateName: `${applicant.firstName} ${applicant.lastName}`,
                      })
                    }
                    onSendAssessment={() => sendAssessmentMutation.mutate(applicant.applicationId)}
                    onScoreAssessment={() => setScoringAssessment(applicant.assessment)}
                    onOpenOffer={() =>
                      setOfferFor({
                        applicationId: applicant.applicationId,
                        candidateName: `${applicant.firstName} ${applicant.lastName}`,
                      })
                    }
                    onOpenBgv={() =>
                      setBgvFor({
                        applicationId: applicant.applicationId,
                        candidateName: `${applicant.firstName} ${applicant.lastName}`,
                      })
                    }
                    onOpenPreboarding={() =>
                      setPreboardingFor({
                        applicationId: applicant.applicationId,
                        candidateName: `${applicant.firstName} ${applicant.lastName}`,
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

      <InterviewsSheet
        applicationId={interviewsFor?.applicationId ?? null}
        candidateName={interviewsFor?.candidateName ?? null}
        onOpenChange={(open) => !open && setInterviewsFor(null)}
      />

      <OfferSheet
        applicationId={offerFor?.applicationId ?? null}
        candidateName={offerFor?.candidateName ?? null}
        onOpenChange={(open) => !open && setOfferFor(null)}
      />

      <BgvSheet
        applicationId={bgvFor?.applicationId ?? null}
        candidateName={bgvFor?.candidateName ?? null}
        onOpenChange={(open) => !open && setBgvFor(null)}
      />

      <PreboardingSheet
        applicationId={preboardingFor?.applicationId ?? null}
        candidateName={preboardingFor?.candidateName ?? null}
        onOpenChange={(open) => !open && setPreboardingFor(null)}
      />

      <ScoreAssessmentDialog
        assessment={scoringAssessment}
        onOpenChange={(open) => !open && setScoringAssessment(null)}
        onSubmit={(score, comments) =>
          scoringAssessment && scoreMutation.mutate({ assessmentSubmissionId: scoringAssessment.id, score, comments })
        }
        isSubmitting={scoreMutation.isPending}
        submissionText={assessmentDetail?.submissionText}
      />
    </div>
  )
}

function ApplicantCard({
  applicant, isAssessmentEnabled, onStageChange, onToggleTalentPool, onOpenInterviews, onSendAssessment,
  onScoreAssessment, onOpenOffer, onOpenBgv, onOpenPreboarding,
}: {
  applicant: ApplicantListItem
  isAssessmentEnabled: boolean
  onStageChange: (stage: ApplicationStage) => void
  onToggleTalentPool: () => void
  onOpenInterviews: () => void
  onSendAssessment: () => void
  onScoreAssessment: () => void
  onOpenOffer: () => void
  onOpenBgv: () => void
  onOpenPreboarding: () => void
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

      {isAssessmentEnabled && (
        <div className="flex items-center gap-2">
          {!applicant.assessment ? (
            <Button type="button" variant="outline" size="sm" className="h-8 text-xs" onClick={onSendAssessment}>
              <FileCheck2 className="size-3.5" />
              Send test
            </Button>
          ) : applicant.assessment.score === null ? (
            applicant.assessment.submittedAt ? (
              <Button type="button" variant="outline" size="sm" className="h-8 text-xs" onClick={onScoreAssessment}>
                <FileCheck2 className="size-3.5" />
                Score test
              </Button>
            ) : (
              <Badge variant="secondary">Test sent - awaiting submission</Badge>
            )
          ) : (
            <Badge variant={applicant.assessment.passed ? "default" : "destructive"}>
              Test: {applicant.assessment.score}/100 - {applicant.assessment.passed ? "Pass" : "Below threshold"}
            </Badge>
          )}
        </div>
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

      <div className="grid grid-cols-2 gap-2">
        <Button type="button" variant="outline" size="sm" className="h-8 text-xs" onClick={onOpenInterviews}>
          <CalendarClock className="size-3.5" />
          Interviews
        </Button>
        <Button type="button" variant="outline" size="sm" className="h-8 text-xs" onClick={onOpenOffer}>
          <Banknote className="size-3.5" />
          Offer
        </Button>
        <Button type="button" variant="outline" size="sm" className="h-8 text-xs" onClick={onOpenBgv}>
          <ShieldCheck className="size-3.5" />
          BGV
        </Button>
        <Button type="button" variant="outline" size="sm" className="h-8 text-xs" onClick={onOpenPreboarding}>
          <ClipboardCheck className="size-3.5" />
          Pre-boarding
        </Button>
      </div>
    </div>
  )
}
