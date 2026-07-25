import type { QualificationLevel, VerificationStatus } from "./types"

export const QUALIFICATION_LEVEL_OPTIONS: { value: QualificationLevel; label: string }[] = [
  { value: "TenthOrBelow", label: "10th or below" },
  { value: "TwelfthOrDiploma", label: "12th / Diploma" },
  { value: "Graduate", label: "Graduate" },
  { value: "PostGraduate", label: "Post-Graduate" },
  { value: "Doctorate", label: "Doctorate" },
  { value: "Other", label: "Other" },
]

export const QUALIFICATION_LEVEL_LABEL: Record<QualificationLevel, string> = Object.fromEntries(
  QUALIFICATION_LEVEL_OPTIONS.map((option) => [option.value, option.label]),
) as Record<QualificationLevel, string>

export const VERIFICATION_STATUS_BADGE_VARIANT: Record<VerificationStatus, "default" | "secondary"> = {
  Pending: "secondary",
  Verified: "default",
}
