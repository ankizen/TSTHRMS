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
  assessment: AssessmentSummary | null
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

export type InterviewRound = "InterviewRound1" | "InterviewRound2" | "InterviewRound3"
export type InterviewStatus = "Scheduled" | "Completed" | "NoShow" | "Cancelled"
export type InterviewRecommendation = "StrongYes" | "Yes" | "No" | "StrongNo"

export interface ScheduleInterviewRequest {
  round: InterviewRound
  scheduledAt: string
  durationMinutes: number
  videoLink: string | null
  panelistUserIds: string[]
}

export interface RescheduleInterviewRequest {
  scheduledAt: string
}

export interface UpdateInterviewStatusRequest {
  status: InterviewStatus
}

export interface SubmitScorecardRequest {
  technicalSkillsRating: number
  communicationRating: number
  problemSolvingRating: number
  cultureFitRating: number
  recommendation: InterviewRecommendation
  comments: string | null
}

export interface InterviewPanelist {
  userId: string
  displayName: string
  hasSubmitted: boolean
}

export interface InterviewScorecard {
  interviewerUserId: string
  interviewerDisplayName: string
  technicalSkillsRating: number
  communicationRating: number
  problemSolvingRating: number
  cultureFitRating: number
  recommendation: InterviewRecommendation
  comments: string | null
  submittedAt: string
}

export interface Interview {
  id: string
  applicationId: string
  round: InterviewRound
  scheduledAt: string
  durationMinutes: number
  videoLink: string | null
  status: InterviewStatus
  rescheduleCount: number
  panelists: InterviewPanelist[]
  visibleScorecards: InterviewScorecard[]
  allScorecardsSubmitted: boolean
  currentUserIsPanelist: boolean
  currentUserHasSubmitted: boolean
}

export interface MyInterview {
  interviewId: string
  applicationId: string
  candidateName: string
  jobPostingTitle: string
  round: InterviewRound
  scheduledAt: string
  durationMinutes: number
  videoLink: string | null
  status: InterviewStatus
  hasSubmitted: boolean
}

export interface InterviewerCandidate {
  userId: string
  email: string
  employeeName: string | null
}

export type AssessmentType = "MachineCodingTest" | "SkillAssignment" | "AptitudeTest" | "CaseStudy"

export interface TestConfigurationRequest {
  isEnabled: boolean
  type: AssessmentType
  instructions: string | null
  timeLimitMinutes: number
  responseWindowDays: number
  passThreshold: number
  retakeCooldownMonths: number
}

export type TestConfiguration = TestConfigurationRequest

export interface AssessmentSummary {
  id: string
  type: AssessmentType
  sentAt: string
  dueAt: string
  submittedAt: string | null
  score: number | null
  passed: boolean | null
  retakeAllowedAfter: string | null
}

export interface AssessmentDetail {
  id: string
  applicationId: string
  type: AssessmentType
  instructions: string | null
  timeLimitMinutes: number
  sentAt: string
  dueAt: string
  submittedAt: string | null
  submissionText: string | null
  submissionDocumentId: string | null
  score: number | null
  passed: boolean | null
  reviewerComments: string | null
  retakeAllowedAfter: string | null
}

export interface ScoreAssessmentRequest {
  score: number
  comments: string | null
}

export interface SendAssessmentResult {
  succeeded: boolean
  error: string | null
  assessment: AssessmentSummary | null
}

export interface PublicAssessment {
  jobTitle: string
  type: AssessmentType
  instructions: string | null
  timeLimitMinutes: number
  dueAt: string
  isExpired: boolean
  alreadySubmitted: boolean
}

export type OfferStatus = "Draft" | "PendingApproval" | "Approved" | "Sent" | "Accepted" | "Declined" | "Expired"

export interface OfferVersion {
  versionNumber: number
  designation: string | null
  dateOfJoining: string
  annualCtc: number
  fixedComponent: number | null
  variableComponent: number | null
  joiningBonus: number | null
  offerLetterText: string | null
  revisionReason: string | null
  createdAt: string
}

export interface Offer {
  id: string
  applicationId: string
  status: OfferStatus
  sentAt: string | null
  expiresAt: string | null
  respondedAt: string | null
  declineReason: string | null
  approvedByUserId: string | null
  approvedAt: string | null
  versions: OfferVersion[]
}

export interface CreateOrReviseOfferRequest {
  designation: string | null
  dateOfJoining: string
  annualCtc: number
  fixedComponent: number | null
  variableComponent: number | null
  joiningBonus: number | null
  offerLetterText: string | null
  revisionReason: string | null
}

export interface SendOfferRequest {
  responseWindowDays: number
}

export type BgvStatus = "NotStarted" | "Initiated" | "InProgress" | "Clear" | "DiscrepancyFound"

export interface Bgv {
  applicationId: string
  status: BgvStatus
  vendorReference: string | null
  isConditionalJoining: boolean
  initiatedAt: string | null
  clearedAt: string | null
  discrepancyNotes: string | null
}

export interface InitiateBgvRequest {
  vendorReference: string | null
  isConditionalJoining: boolean
}

export interface UpdateBgvStatusRequest {
  status: BgvStatus
  notes: string | null
}

export type PreboardingTaskType =
  | "EducationCertificate" | "IdentityProof" | "PreviousEmploymentRelievingLetter"
  | "BankDetails" | "ItAssetRequest" | "WelcomeCommunication"

export type PreboardingTaskStatus = "Pending" | "Completed"

export interface PreboardingChecklistItem {
  id: string
  taskType: PreboardingTaskType
  status: PreboardingTaskStatus
  completedAt: string | null
  documentId: string | null
  bankAccountNumberMasked: string | null
  bankIfscCode: string | null
}

export interface ConvertToEmployeeResult {
  succeeded: boolean
  error: string | null
  employee: { id: string; employeeCode: string; firstName: string; lastName: string } | null
}

export type OnboardingTaskType = "ItSetup" | "AccessProvisioning" | "InductionSession" | "PolicyAcknowledgement" | "BuddyAssignment"
export type OnboardingTaskStatus = "Pending" | "Completed"

export interface OnboardingChecklistItem {
  id: string
  taskType: OnboardingTaskType
  ownerUserId: string | null
  ownerDisplayName: string | null
  dueDate: string
  status: OnboardingTaskStatus
  completedAt: string | null
  isOverdue: boolean
}

export interface UpdateOnboardingItemRequest {
  ownerUserId: string | null
  dueDate: string | null
}

export type { Lookup }
