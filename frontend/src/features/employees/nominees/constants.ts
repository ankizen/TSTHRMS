import type { NominationType } from "./types"

export const NOMINATION_TYPE_OPTIONS: { value: NominationType; label: string }[] = [
  { value: "ProvidentFund", label: "Provident Fund" },
  { value: "Gratuity", label: "Gratuity" },
  { value: "Insurance", label: "Insurance / Mediclaim" },
]

export const NOMINATION_TYPE_LABEL: Record<NominationType, string> = Object.fromEntries(
  NOMINATION_TYPE_OPTIONS.map((option) => [option.value, option.label]),
) as Record<NominationType, string>
