import type { ApplicationStage, RequisitionReason, RequisitionStatus } from "./types"

export const REQUISITION_REASON_OPTIONS: { value: RequisitionReason; label: string }[] = [
  { value: "Backfill", label: "Backfill" },
  { value: "NewRole", label: "New role" },
]

export const REQUISITION_STATUS_LABELS: Record<RequisitionStatus, string> = {
  Draft: "Draft",
  PendingApproval: "Pending Approval",
  Approved: "Approved",
  Rejected: "Rejected",
  OnHold: "On Hold",
  Closed: "Closed",
}

export const REQUISITION_STATUS_BADGE_VARIANT: Record<RequisitionStatus, "default" | "secondary" | "outline" | "destructive"> = {
  Draft: "outline",
  PendingApproval: "secondary",
  Approved: "default",
  Rejected: "destructive",
  OnHold: "secondary",
  Closed: "outline",
}

export const APPLICATION_STAGE_OPTIONS: { value: ApplicationStage; label: string }[] = [
  { value: "Applied", label: "Applied" },
  { value: "Screening", label: "Screening" },
  { value: "Assessment", label: "Assessment" },
  { value: "InterviewRound1", label: "Interview Round 1" },
  { value: "InterviewRound2", label: "Interview Round 2" },
  { value: "InterviewRound3", label: "Interview Round 3 (Final)" },
  { value: "Selected", label: "Selected" },
  { value: "Offer", label: "Offer" },
  { value: "OfferAccepted", label: "Offer Accepted" },
  { value: "Hired", label: "Hired" },
  { value: "Rejected", label: "Rejected" },
  { value: "OnHold", label: "On Hold" },
]

export const APPLICATION_STAGE_LABELS: Record<ApplicationStage, string> = Object.fromEntries(
  APPLICATION_STAGE_OPTIONS.map((option) => [option.value, option.label]),
) as Record<ApplicationStage, string>
