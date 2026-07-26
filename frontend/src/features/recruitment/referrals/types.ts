import type { ApplicationStage } from "../types"

export interface OpenJobOption {
  slug: string
  title: string
  department: string | null
  location: string | null
}

export interface ReferralSubmissionRequest {
  firstName: string
  lastName: string
  email: string
  phone: string
}

export interface ApplyResult {
  succeeded: boolean
  error: string | null
  applicationId: string | null
}

export type ReferralBonusStatus = "NotApplicable" | "Payable" | "Paid"

export interface MyReferral {
  candidateId: string
  candidateName: string
  jobPostingTitle: string
  stage: ApplicationStage
  appliedAt: string
  referralBonusStatus: ReferralBonusStatus
  referralBonusAmount: number | null
}

export interface ReferralPayout {
  candidateId: string
  candidateName: string
  referredByEmployeeName: string
  jobPostingTitle: string
  bonusAmount: number
  status: ReferralBonusStatus
  paidAt: string | null
}
