import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { ArrowLeft, Star, UsersRound } from "lucide-react"
import { useState } from "react"
import { toast } from "sonner"
import { useNavigate, useParams } from "react-router-dom"
import { EmptyState } from "@/components/empty-state"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { TableSkeletonRows } from "@/components/table-skeleton-rows"
import { getApplicants, moveApplicationStage, setTalentPool } from "./api"
import { APPLICATION_STAGE_OPTIONS } from "./constants"
import { MoveStageDialog } from "./move-stage-dialog"
import type { ApplicationStage } from "./types"

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

      <div className="rounded-xl border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Candidate</TableHead>
              <TableHead>Contact</TableHead>
              <TableHead>CTC (current / expected)</TableHead>
              <TableHead>Notice</TableHead>
              <TableHead>Source</TableHead>
              <TableHead>Stage</TableHead>
              <TableHead className="text-right">Talent pool</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              <TableSkeletonRows columns={7} />
            ) : !applicants || applicants.length === 0 ? (
              <TableRow>
                <TableCell colSpan={7}>
                  <EmptyState
                    icon={UsersRound}
                    title="No applicants yet"
                    description="Candidates who apply from the career site will show up here."
                  />
                </TableCell>
              </TableRow>
            ) : (
              applicants.map((applicant) => (
                <TableRow key={applicant.applicationId}>
                  <TableCell>
                    <div className="flex flex-col">
                      <span className="font-medium">{applicant.firstName} {applicant.lastName}</span>
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
                    </div>
                  </TableCell>
                  <TableCell className="text-sm text-muted-foreground">
                    <div>{applicant.email}</div>
                    <div>{applicant.phone}</div>
                  </TableCell>
                  <TableCell>
                    {applicant.currentCtc ?? "-"} / {applicant.expectedCtc ?? "-"}
                  </TableCell>
                  <TableCell>{applicant.noticePeriodDays ?? "-"}</TableCell>
                  <TableCell>
                    <Badge variant="secondary">{applicant.source}</Badge>
                  </TableCell>
                  <TableCell>
                    <Select
                      value={applicant.stage}
                      onValueChange={(value) => handleStageChange(applicant.applicationId, value as ApplicationStage)}
                    >
                      <SelectTrigger className="w-[180px]">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        {APPLICATION_STAGE_OPTIONS.map((option) => (
                          <SelectItem key={option.value} value={option.value}>{option.label}</SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    {applicant.stage === "Rejected" && applicant.rejectionReason && (
                      <p className="mt-1 max-w-[180px] text-xs text-muted-foreground">{applicant.rejectionReason}</p>
                    )}
                  </TableCell>
                  <TableCell className="text-right">
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon"
                      onClick={() => talentPoolMutation.mutate({
                        candidateId: applicant.candidateId,
                        isInTalentPool: !applicant.isInTalentPool,
                      })}
                    >
                      <Star className={applicant.isInTalentPool ? "fill-amber-400 text-amber-400" : ""} />
                    </Button>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

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
