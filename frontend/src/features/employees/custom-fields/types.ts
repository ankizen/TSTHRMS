import type { CustomFieldType } from "@/features/admin/custom-fields/types"

export interface EmployeeCustomFieldValue {
  definitionId: string
  name: string
  label: string
  fieldType: CustomFieldType
  options: string[] | null
  isRequired: boolean
  value: string | null
}
