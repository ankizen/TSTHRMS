import type {
  ApplicationStage,
  InterviewRecommendation,
  InterviewRound,
  InterviewStatus,
  RequisitionReason,
  RequisitionStatus,
} from "./types"

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

export const INTERVIEW_ROUND_OPTIONS: { value: InterviewRound; label: string }[] = [
  { value: "InterviewRound1", label: "Interview Round 1" },
  { value: "InterviewRound2", label: "Interview Round 2" },
  { value: "InterviewRound3", label: "Interview Round 3 (Final)" },
]

export const INTERVIEW_STATUS_LABELS: Record<InterviewStatus, string> = {
  Scheduled: "Scheduled",
  Completed: "Completed",
  NoShow: "No-show",
  Cancelled: "Cancelled",
}

export const INTERVIEW_STATUS_BADGE_VARIANT: Record<InterviewStatus, "default" | "secondary" | "outline" | "destructive"> = {
  Scheduled: "default",
  Completed: "secondary",
  NoShow: "destructive",
  Cancelled: "outline",
}

export const INTERVIEW_RECOMMENDATION_OPTIONS: { value: InterviewRecommendation; label: string }[] = [
  { value: "StrongYes", label: "Strong Yes" },
  { value: "Yes", label: "Yes" },
  { value: "No", label: "No" },
  { value: "StrongNo", label: "Strong No" },
]

export const INTERVIEW_RECOMMENDATION_LABELS: Record<InterviewRecommendation, string> = Object.fromEntries(
  INTERVIEW_RECOMMENDATION_OPTIONS.map((option) => [option.value, option.label]),
) as Record<InterviewRecommendation, string>
