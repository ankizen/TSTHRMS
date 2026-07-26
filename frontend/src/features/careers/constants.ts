import type {
  PreboardingTaskType,
  PublicApplicationStage,
  PublicAssessmentType,
  PublicEmploymentType,
  PublicInterviewStatus,
  PublicOfferStatus,
} from "./types"

export const EMPLOYMENT_TYPE_LABELS: Record<PublicEmploymentType, string> = {
  FullTime: "Full-time",
  Contract: "Contract",
  Intern: "Internship",
}

export const ASSESSMENT_TYPE_LABELS: Record<PublicAssessmentType, string> = {
  MachineCodingTest: "Coding Test",
  SkillAssignment: "Skill Assignment",
  AptitudeTest: "Aptitude Test",
  CaseStudy: "Case Study",
}

export const APPLICATION_STAGE_LABELS: Record<PublicApplicationStage, string> = {
  Applied: "Applied",
  Screening: "Screening",
  Assessment: "Assessment",
  InterviewRound1: "Interview Round 1",
  InterviewRound2: "Interview Round 2",
  InterviewRound3: "Interview Round 3 (Final)",
  Selected: "Selected",
  Offer: "Offer",
  OfferAccepted: "Offer Accepted",
  Hired: "Hired",
  Rejected: "Not Moving Forward",
  OnHold: "On Hold",
}

export const INTERVIEW_STATUS_LABELS: Record<PublicInterviewStatus, string> = {
  Scheduled: "Scheduled",
  Completed: "Completed",
  NoShow: "No-show",
  Cancelled: "Cancelled",
}

export const OFFER_STATUS_LABELS: Record<PublicOfferStatus, string> = {
  Draft: "Being prepared",
  PendingApproval: "Being prepared",
  Approved: "Being prepared",
  Sent: "Awaiting your response",
  Accepted: "Accepted",
  Declined: "Declined",
  Expired: "Expired",
}

export const PREBOARDING_TASK_LABELS: Record<PreboardingTaskType, string> = {
  EducationCertificate: "Education Certificate",
  IdentityProof: "Identity Proof",
  PreviousEmploymentRelievingLetter: "Previous Employment Relieving Letter",
  BankDetails: "Bank Details",
  ItAssetRequest: "IT Setup",
  WelcomeCommunication: "Welcome Email",
}
