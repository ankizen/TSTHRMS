import type { PublicAssessmentType, PublicEmploymentType } from "./types"

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
