import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import {
  ArrowLeft, CheckCircle2, ExternalLink, Pause, Pencil, Play, Users, XCircle,
} from "lucide-react"
import { useState } from "react"
import { toast } from "sonner"
import { Link, useNavigate, useParams } from "react-router-dom"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Skeleton } from "@/components/ui/skeleton"
import { useAuthStore } from "@/stores/auth-store"
import {
  approveRequisition, closeRequisition, getCurrentTenant, getRequisition,
  holdRequisition, publishRequisition, rejectRequisition, resumeRequisition,
  submitRequisition, updateRequisition,
} from "./api"
import { REQUISITION_STATUS_BADGE_VARIANT, REQUISITION_STATUS_LABELS } from "./constants"
import { DecisionDialog } from "./decision-dialog"
import { PublishPostingDialog } from "./publish-posting-dialog"
import { RequisitionFormDialog } from "./requisition-form-dialog"
import type { JobRequisitionWriteRequest } from "./types"

export function RequisitionDetailPage() {
  const { id = "" } = useParams()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const roles = useAuthStore((s) => s.user?.roles ?? [])
  const isHr = roles.includes("HRAdmin") || roles.includes("HRBP")

  const [editOpen, setEditOpen] = useState(false)
  const [publishOpen, setPublishOpen] = useState(false)
  const [approveOpen, setApproveOpen] = useState(false)
  const [rejectOpen, setRejectOpen] = useState(false)

  const queryKey = ["recruitment", "requisitions", id]
  const { data: requisition, isLoading } = useQuery({
    queryKey, queryFn: () => getRequisition(id), enabled: Boolean(id),
  })

  const { data: tenant } = useQuery({ queryKey: ["tenant", "current"], queryFn: getCurrentTenant })

  const invalidate = () => queryClient.invalidateQueries({ queryKey })

  const withToast = (label: string) => ({
    onSuccess: async () => {
      toast.success(label)
      await invalidate()
    },
    onError: () => toast.error("That action couldn't be completed."),
  })

  const updateMutation = useMutation({
    mutationFn: (request: JobRequisitionWriteRequest) => updateRequisition(id, request),
    onSuccess: async () => {
      toast.success("Requisition updated.")
      setEditOpen(false)
      await invalidate()
    },
    onError: () => toast.error("Couldn't update the requisition."),
  })
  const submitMutation = useMutation({ mutationFn: () => submitRequisition(id), ...withToast("Submitted for approval.") })
  const approveMutation = useMutation({
    mutationFn: (comment: string | null) => approveRequisition(id, comment),
    onSuccess: async () => { toast.success("Requisition approved."); setApproveOpen(false); await invalidate() },
    onError: () => toast.error("Couldn't approve the requisition."),
  })
  const rejectMutation = useMutation({
    mutationFn: (comment: string | null) => rejectRequisition(id, comment),
    onSuccess: async () => { toast.success("Requisition rejected."); setRejectOpen(false); await invalidate() },
    onError: () => toast.error("Couldn't reject the requisition."),
  })
  const holdMutation = useMutation({ mutationFn: () => holdRequisition(id), ...withToast("Requisition put on hold.") })
  const resumeMutation = useMutation({ mutationFn: () => resumeRequisition(id), ...withToast("Requisition resumed.") })
  const closeMutation = useMutation({ mutationFn: () => closeRequisition(id), ...withToast("Requisition closed.") })
  const publishMutation = useMutation({
    mutationFn: (request: Parameters<typeof publishRequisition>[1]) => publishRequisition(id, request),
    onSuccess: async () => { toast.success("Published to the career site."); setPublishOpen(false); await invalidate() },
    onError: () => toast.error("Couldn't publish - a job description is required."),
  })

  if (isLoading || !requisition) {
    return (
      <div className="flex flex-col gap-4">
        <Skeleton className="h-8 w-1/2" />
        <Skeleton className="h-32 w-full" />
      </div>
    )
  }

  const canEdit = requisition.status === "Draft" || requisition.status === "Rejected"
  const canSubmit = requisition.status === "Draft"
  const canDecide = isHr && requisition.status === "PendingApproval"
  const canHold = isHr && requisition.status === "Approved"
  const canResume = isHr && requisition.status === "OnHold"
  const canClose = isHr && (requisition.status === "Approved" || requisition.status === "OnHold")
  const canPublish = isHr && requisition.status === "Approved" && !requisition.jobPosting?.isPublished

  const careerSiteUrl = tenant && requisition.jobPosting
    ? `${window.location.origin}/careers/${tenant.slug}/${requisition.jobPosting.slug}`
    : null

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-3">
        <Button variant="ghost" size="sm" className="w-fit" onClick={() => navigate("/recruitment/requisitions")}>
          <ArrowLeft />
          Back to requisitions
        </Button>
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <p className="font-mono text-xs text-muted-foreground">{requisition.requisitionCode}</p>
            <h1 className="text-2xl font-semibold tracking-tight">{requisition.title}</h1>
          </div>
          <Badge variant={REQUISITION_STATUS_BADGE_VARIANT[requisition.status]} className="text-sm">
            {REQUISITION_STATUS_LABELS[requisition.status]}
          </Badge>
        </div>
      </div>

      <div className="grid grid-cols-2 gap-4 rounded-xl border p-5 sm:grid-cols-4">
        <Field label="Entity" value={requisition.legalEntityName} />
        <Field label="Product" value={requisition.productName} />
        <Field label="Department" value={requisition.department ?? "-"} />
        <Field label="Grade" value={requisition.grade ?? "-"} />
        <Field label="Employment type" value={requisition.employmentType} />
        <Field label="Openings" value={String(requisition.openings)} />
        <Field label="Budget/opening" value={requisition.budgetPerOpening ? `₹${requisition.budgetPerOpening.toLocaleString()}` : "-"} />
        <Field label="Interview rounds" value={String(requisition.interviewRoundCount)} />
        <Field label="Reason" value={requisition.reason === "NewRole" ? "New role" : "Backfill"} />
      </div>

      {requisition.justificationNotes && (
        <div className="rounded-xl border p-5">
          <p className="mb-1 text-sm font-medium">Justification</p>
          <p className="text-sm text-muted-foreground">{requisition.justificationNotes}</p>
        </div>
      )}

      <div className="flex flex-wrap gap-2">
        {canEdit && (
          <Button variant="outline" onClick={() => setEditOpen(true)}>
            <Pencil />
            Edit
          </Button>
        )}
        {canSubmit && (
          <Button onClick={() => submitMutation.mutate()} disabled={submitMutation.isPending}>
            Submit for approval
          </Button>
        )}
        {canDecide && (
          <>
            <Button onClick={() => setApproveOpen(true)}>
              <CheckCircle2 />
              Approve
            </Button>
            <Button variant="destructive" onClick={() => setRejectOpen(true)}>
              <XCircle />
              Reject
            </Button>
          </>
        )}
        {canHold && (
          <Button variant="outline" onClick={() => holdMutation.mutate()} disabled={holdMutation.isPending}>
            <Pause />
            Put on hold
          </Button>
        )}
        {canResume && (
          <Button variant="outline" onClick={() => resumeMutation.mutate()} disabled={resumeMutation.isPending}>
            <Play />
            Resume
          </Button>
        )}
        {canPublish && (
          <Button onClick={() => setPublishOpen(true)}>
            {requisition.jobPosting ? "Republish to career site" : "Publish to career site"}
          </Button>
        )}
        {canClose && (
          <Button variant="outline" onClick={() => closeMutation.mutate()} disabled={closeMutation.isPending}>
            Close requisition
          </Button>
        )}
        {requisition.jobPosting && (
          <Button variant="outline" asChild>
            <Link to={`/recruitment/postings/${requisition.jobPosting.id}/applicants`}>
              <Users />
              View applicants ({requisition.jobPosting.applicationCount})
            </Link>
          </Button>
        )}
      </div>

      {careerSiteUrl && requisition.jobPosting?.isPublished && (
        <div className="flex items-center gap-2 rounded-xl border bg-muted/40 px-4 py-3 text-sm">
          <span className="text-muted-foreground">Live at</span>
          <a href={careerSiteUrl} target="_blank" rel="noreferrer" className="flex items-center gap-1 font-medium text-primary hover:underline">
            {careerSiteUrl}
            <ExternalLink className="size-3.5" />
          </a>
        </div>
      )}

      {requisition.approvals.length > 0 && (
        <div className="rounded-xl border p-5">
          <p className="mb-3 text-sm font-medium">Approval history</p>
          <div className="flex flex-col gap-3">
            {requisition.approvals.map((approval) => (
              <div key={approval.id} className="flex items-start justify-between gap-3 text-sm">
                <div>
                  <Badge variant={approval.decision === "Approved" ? "default" : "destructive"}>
                    {approval.decision}
                  </Badge>
                  {approval.comment && <p className="mt-1 text-muted-foreground">{approval.comment}</p>}
                </div>
                <span className="shrink-0 text-xs text-muted-foreground">
                  {new Date(approval.decidedAt).toLocaleString()}
                </span>
              </div>
            ))}
          </div>
        </div>
      )}

      <RequisitionFormDialog
        open={editOpen}
        onOpenChange={setEditOpen}
        onSubmit={(request) => updateMutation.mutate(request)}
        isSubmitting={updateMutation.isPending}
        requisition={requisition}
      />
      <PublishPostingDialog
        open={publishOpen}
        onOpenChange={setPublishOpen}
        onSubmit={(request) => publishMutation.mutate(request)}
        isSubmitting={publishMutation.isPending}
        existingPosting={requisition.jobPosting}
      />
      <DecisionDialog
        open={approveOpen}
        onOpenChange={setApproveOpen}
        onSubmit={(comment) => approveMutation.mutate(comment)}
        isSubmitting={approveMutation.isPending}
        title="Approve requisition"
        actionLabel="Approve"
      />
      <DecisionDialog
        open={rejectOpen}
        onOpenChange={setRejectOpen}
        onSubmit={(comment) => rejectMutation.mutate(comment)}
        isSubmitting={rejectMutation.isPending}
        title="Reject requisition"
        actionLabel="Reject"
        variant="destructive"
      />
    </div>
  )
}

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-xs text-muted-foreground">{label}</p>
      <p className="text-sm font-medium">{value}</p>
    </div>
  )
}
