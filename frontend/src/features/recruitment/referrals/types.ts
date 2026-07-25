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

export interface MyReferral {
  candidateId: string
  candidateName: string
  jobPostingTitle: string
  stage: ApplicationStage
  appliedAt: string
}
