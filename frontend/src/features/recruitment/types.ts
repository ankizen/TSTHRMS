import type { EmploymentType, Lookup } from "@/features/employees/types"

export type RequisitionReason = "Backfill" | "NewRole"
export type RequisitionStatus = "Draft" | "PendingApproval" | "Approved" | "Rejected" | "OnHold" | "Closed"
export type RequisitionApprovalDecision = "Approved" | "Rejected"

export type ApplicationStage =
  | "Applied" | "Screening" | "Assessment"
  | "InterviewRound1" | "InterviewRound2" | "InterviewRound3"
  | "Selected" | "Offer" | "OfferAccepted" | "Hired" | "Rejected" | "OnHold"

export type CandidateSource =
  | "CareerSite" | "Referral" | "LinkedIn" | "Naukri" | "Indeed" | "WalkIn" | "CampusDrive"

export interface JobRequisitionListItem {
  id: string
  requisitionCode: string
  title: string
  legalEntityName: string
  productName: string
  openings: number
  reason: RequisitionReason
  status: RequisitionStatus
  hasJobPosting: boolean
  isPublished: boolean
  createdAt: string
}

export interface RequisitionApproval {
  id: string
  approverUserId: string
  decision: RequisitionApprovalDecision
  comment: string | null
  decidedAt: string
}

export interface JobPosting {
  id: string
  title: string
  slug: string
  description: string
  department: string | null
  location: string | null
  employmentType: EmploymentType
  isPublished: boolean
  publishedAt: string | null
  closedAt: string | null
  applicationCount: number
}

export interface JobRequisition {
  id: string
  requisitionCode: string
  title: string
  legalEntityId: string
  legalEntityName: string
  productId: string
  productName: string
  grade: string | null
  department: string | null
  employmentType: EmploymentType
  openings: number
  budgetPerOpening: number | null
  reason: RequisitionReason
  justificationNotes: string | null
  interviewRoundCount: number
  status: RequisitionStatus
  raisedByUserId: string
  approvedAt: string | null
  closedAt: string | null
  jobPosting: JobPosting | null
  approvals: RequisitionApproval[]
}

export interface JobRequisitionWriteRequest {
  title: string
  legalEntityId: string
  productId: string
  grade: string | null
  department: string | null
  employmentType: EmploymentType
  openings: number
  budgetPerOpening: number | null
  reason: RequisitionReason
  justificationNotes: string | null
  interviewRoundCount: number
}

export interface PublishJobPostingRequest {
  description: string | null
  location: string | null
}

export interface CandidateOtherApplication {
  applicationId: string
  jobPostingId: string
  jobPostingTitle: string
  stage: ApplicationStage
  appliedAt: string
}

export interface ApplicantListItem {
  applicationId: string
  candidateId: string
  firstName: string
  lastName: string
  email: string
  phone: string
  resumeDocumentId: string | null
  currentCtc: number | null
  expectedCtc: number | null
  noticePeriodDays: number | null
  source: CandidateSource
  isInTalentPool: boolean
  stage: ApplicationStage
  stageChangedAt: string
  rejectionReason: string | null
  appliedAt: string
  otherApplications: CandidateOtherApplication[]
}

export interface MoveApplicationStageRequest {
  stage: ApplicationStage
  reason: string | null
}

export interface TalentPoolCandidate {
  candidateId: string
  firstName: string
  lastName: string
  email: string
  phone: string
  resumeDocumentId: string | null
  mostRecentJobPostingTitle: string | null
  mostRecentStage: ApplicationStage | null
  mostRecentAppliedAt: string | null
}

export type { Lookup }
