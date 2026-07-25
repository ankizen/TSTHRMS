export type PublicEmploymentType = "FullTime" | "Contract" | "Intern"

export interface PublicCompany {
  name: string
}

export interface PublicJobListItem {
  slug: string
  title: string
  department: string | null
  location: string | null
  employmentType: PublicEmploymentType
  legalEntityName: string
  productName: string
  publishedAt: string
}

export interface PublicJobDetail {
  slug: string
  title: string
  description: string
  department: string | null
  location: string | null
  employmentType: PublicEmploymentType
  legalEntityName: string
  productName: string
  publishedAt: string
}

export interface PublicJobFilter {
  legalEntityId?: string
  productId?: string
  location?: string
  department?: string
}

export interface PublicApplicationRequest {
  firstName: string
  lastName: string
  email: string
  phone: string
  currentCtc: number | null
  expectedCtc: number | null
  noticePeriodDays: number | null
  consentGiven: boolean
}

export interface ApplyResult {
  succeeded: boolean
  error: string | null
  applicationId: string | null
}

export type PublicAssessmentType = "MachineCodingTest" | "SkillAssignment" | "AptitudeTest" | "CaseStudy"

export interface PublicAssessment {
  jobTitle: string
  type: PublicAssessmentType
  instructions: string | null
  timeLimitMinutes: number
  dueAt: string
  isExpired: boolean
  alreadySubmitted: boolean
}

export type PublicOfferStatus = "Draft" | "PendingApproval" | "Approved" | "Sent" | "Accepted" | "Declined" | "Expired"

export interface PublicOffer {
  candidateName: string
  jobTitle: string
  designation: string | null
  dateOfJoining: string
  annualCtc: number
  fixedComponent: number | null
  variableComponent: number | null
  joiningBonus: number | null
  offerLetterText: string | null
  expiresAt: string
  isExpired: boolean
  status: PublicOfferStatus
}

export type PublicApplicationStage =
  | "Applied" | "Screening" | "Assessment"
  | "InterviewRound1" | "InterviewRound2" | "InterviewRound3"
  | "Selected" | "Offer" | "OfferAccepted" | "Hired" | "Rejected" | "OnHold"

export type PublicInterviewStatus = "Scheduled" | "Completed" | "NoShow" | "Cancelled"

export interface CandidateLoginResult {
  succeeded: boolean
  accessToken: string | null
  expiresAt: string | null
  candidateName: string | null
}

export interface MyApplicationInterview {
  interviewId: string
  round: PublicApplicationStage
  scheduledAt: string
  durationMinutes: number
  videoLink: string | null
  status: PublicInterviewStatus
}

export interface MyApplicationAssessment {
  type: PublicAssessmentType
  dueAt: string
  submitted: boolean
}

export interface MyApplicationOffer {
  status: PublicOfferStatus
  offerToken: string | null
}

export interface MyApplication {
  applicationId: string
  jobPostingTitle: string
  stage: PublicApplicationStage
  appliedAt: string
  interviews: MyApplicationInterview[]
  assessment: MyApplicationAssessment | null
  offer: MyApplicationOffer | null
}
