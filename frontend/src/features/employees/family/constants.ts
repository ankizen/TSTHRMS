import type { FamilyRelation } from "./types"

export const FAMILY_RELATION_OPTIONS: { value: FamilyRelation; label: string }[] = [
  { value: "Spouse", label: "Spouse" },
  { value: "Parent", label: "Parent" },
  { value: "Child", label: "Child" },
  { value: "Other", label: "Other" },
]

export const FAMILY_RELATION_LABEL: Record<FamilyRelation, string> = Object.fromEntries(
  FAMILY_RELATION_OPTIONS.map((option) => [option.value, option.label]),
) as Record<FamilyRelation, string>
