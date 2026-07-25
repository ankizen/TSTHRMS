import type { CustomFieldType } from "./types"

export const FIELD_TYPE_OPTIONS: { value: CustomFieldType; label: string }[] = [
  { value: "Text", label: "Text" },
  { value: "Number", label: "Number" },
  { value: "Date", label: "Date" },
  { value: "Boolean", label: "Yes/No" },
  { value: "Select", label: "Dropdown" },
]
